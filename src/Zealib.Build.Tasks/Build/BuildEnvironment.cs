using System;
using System.Collections;
using System.Dynamic;
using System.Reflection;
using Microsoft.Build.Execution;

namespace Zealib.Build
{
    public class BuildEnvironment
    {
        private class DynamicReflectionObject : DynamicObject
        {
            private readonly object m_Object;

            public DynamicReflectionObject(object obj)
            {
                m_Object = obj;
            }

            public override bool TryInvokeMember(
                    InvokeMemberBinder binder, object[] args, out object result)
            {
                var info = m_Object.GetType().GetMethod(
                    binder.Name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                result = info.Invoke(m_Object, args);
                return true;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                var property = m_Object.GetType().GetProperty(
                    binder.Name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var field = m_Object.GetType().GetField(
                    binder.Name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                result = null;
                if (property != null)
                    result = property.GetValue(m_Object, null);
                else if (field != null)
                    result = field.GetValue(m_Object);
                else
                    return false;
                return true;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                var property = m_Object.GetType().GetProperty(
                    binder.Name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var field = m_Object.GetType().GetField(
                    binder.Name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (property != null)
                    property.SetValue(m_Object, value, null);
                else if (field != null)
                    field.SetValue(m_Object, value);
                else
                    return false;

                return true;
            }
        }

        private readonly BuildManager m_Manager;

        public BuildEnvironment()
            : this(BuildManager.DefaultBuildManager)
        {
        }

        public BuildEnvironment(BuildManager manager)
        {
            if (manager == null) throw new ArgumentNullException("manager");
            m_Manager = manager;
        }

        public int CurrentNodeId
        {
            get
            {
                dynamic parameters = new DynamicReflectionObject(BuildParameters);
                return parameters.NodeId;
            }
        }

        public ProjectInstance CurrentProject
        {
            get
            {
                dynamic man = new DynamicReflectionObject(m_Manager);
                dynamic scheduler = new DynamicReflectionObject(man.scheduler);
                foreach (var e in man.configCache)
                {
                    dynamic config = new DynamicReflectionObject(e);
                    if (scheduler.IsCurrentlyBuildingConfiguration(config.ConfigurationId))
                        return config.Project;
                }
                return null;
            }
        }

        public BuildParameters BuildParameters
        {
            get
            {
                dynamic man = new DynamicReflectionObject(m_Manager);
                return man.buildParameters;
            }
        }

    }
}
