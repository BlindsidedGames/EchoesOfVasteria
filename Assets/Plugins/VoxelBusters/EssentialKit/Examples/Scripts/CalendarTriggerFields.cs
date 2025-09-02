using UnityEngine;
using UnityEngine.UI;
// key namespaces
using VoxelBusters.CoreLibrary.NativePlugins;
// internal namespace

namespace VoxelBusters.EssentialKit.Demo
{
    public class CalendarTriggerFields : MonoBehaviour
    {
        [SerializeField]
        private InputField m_day;

        [SerializeField]
        private InputField m_month;

        [SerializeField]
        private InputField m_year;

        [SerializeField]
        private InputField m_hour;

        [SerializeField]
        private InputField m_minute;

        [SerializeField]
        private InputField m_second;

        [SerializeField]
        private InputField m_dayOfWeek;

        [SerializeField]
        private InputField m_weekOfMonth;

        [SerializeField]
        private InputField m_weekOfYear;

        public DateComponents GetDateComponents()
        {
            DateComponents dateComponents = new DateComponents();

            if(!string.IsNullOrEmpty(m_day.text))
                dateComponents.Day = int.Parse(m_day.text);

            if(!string.IsNullOrEmpty(m_month.text))
                dateComponents.Month = int.Parse(m_month.text);

            if(!string.IsNullOrEmpty(m_year.text))
                dateComponents.Year = int.Parse(m_year.text);
            
            if(!string.IsNullOrEmpty(m_hour.text))
                dateComponents.Hour = int.Parse(m_hour.text);

            if(!string.IsNullOrEmpty(m_minute.text))
                dateComponents.Minute = int.Parse(m_minute.text);

            if(!string.IsNullOrEmpty(m_second.text))
                dateComponents.Second = int.Parse(m_second.text);
                
            if(!string.IsNullOrEmpty(m_dayOfWeek.text))
                dateComponents.DayOfWeek = int.Parse(m_dayOfWeek.text);

            if(!string.IsNullOrEmpty(m_weekOfMonth.text))
                dateComponents.WeekOfMonth = int.Parse(m_weekOfMonth.text);

            if(!string.IsNullOrEmpty(m_weekOfYear.text))
                dateComponents.WeekOfYear = int.Parse(m_weekOfYear.text);

            return dateComponents;
        }

    }
}
