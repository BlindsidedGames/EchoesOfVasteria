using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.NotificationServicesCore
{
    public abstract class NotificationBase : NativeObjectBase, INotification
    {
        #region Fields

        [SerializeField]
        private     string          m_id;

        #endregion

        #region Constructors

        protected NotificationBase(string id)
        {
            // set properties
            m_id    = id;
        }

        #endregion

        #region Abstract methods

        protected abstract string GetTitleInternal();

        protected abstract string GetSubtitleInternal();

        protected abstract string GetBodyInternal();

        protected abstract int GetBadgeInternal();

        protected abstract IDictionary GetUserInfoInternal();

        protected abstract string GetSoundFileNameInternal();
        
        protected abstract INotificationTrigger GetTriggerInternal();

        protected abstract bool GetIsLaunchNotificationInternal();

        protected abstract NotificationIosProperties GetIosPropertiesInternal();
        
        protected abstract NotificationAndroidProperties GetAndroidPropertiesInternal();

        #endregion

        #region Base class methods

        public override string ToString()
        {
            var     sb  = new StringBuilder();
            sb.Append("Notification { ");
            sb.Append("Id: ").Append(Id).Append(" ");
            sb.Append("}");
            return sb.ToString();
        }

        #endregion

        #region INotification implementation

        public string Id => m_id;

        public string Title => GetTitleInternal();

        public string Subtitle => GetSubtitleInternal();

        public string Body => GetBodyInternal();

        public int Badge => GetBadgeInternal();

        public IDictionary UserInfo => GetUserInfoInternal();

        public string SoundFileName => GetSoundFileNameInternal();

        public NotificationTriggerType TriggerType
        {
            get
            {
                INotificationTrigger    trigger     = Trigger;
                if (trigger is TimeIntervalNotificationTrigger)
                {
                    return NotificationTriggerType.TimeInterval;
                }
                if (trigger is CalendarNotificationTrigger)
                {
                    return NotificationTriggerType.Calendar;
                }
                if (trigger is LocationNotificationTrigger)
                {
                    return NotificationTriggerType.Location;
                }
                if (trigger is PushNotificationTrigger)
                {
                    return NotificationTriggerType.PushNotification;
                }

                return NotificationTriggerType.Undefined;
            }
        }

        public INotificationTrigger Trigger => GetTriggerInternal();

        public bool IsLaunchNotification => GetIsLaunchNotificationInternal();

        public NotificationIosProperties IosProperties => GetIosPropertiesInternal();

        public NotificationAndroidProperties AndroidProperties => GetAndroidPropertiesInternal();

        #endregion
    }
}