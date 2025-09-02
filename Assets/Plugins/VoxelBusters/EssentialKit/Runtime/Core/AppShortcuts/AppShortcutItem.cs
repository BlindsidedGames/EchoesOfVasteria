namespace VoxelBusters.EssentialKit
{
    
    /// <summary>
    /// Represents an app shortcut item.
    /// </summary>
    ///@ingroup AppShortcuts
    public class AppShortcutItem
    {
        public string Identifier { get; private set; }
        public string Title { get; private set; }
        
        public string Subtitle { get; private set; }
        public string IconFileName { get; private set; }
        
        /// <summary>
        ///  Builder for creating a new instance of the <see cref="AppShortcutItem"/> class.
        /// </summary>
        public class Builder
        {
            private readonly string     m_identifier;
            private readonly string     m_title;
            private string              m_subtitle;
            private string              m_iconFileName;
            
            /// <summary>
            ///  Constructor for creating a new instance of the <see cref="AppShortcutItem.Builder"/> class.
            /// </summary>
            /// <param name="identifier">Unique identifier to identify this shortcut</param>
            /// <param name="title">Title of the shortcut</param>
            public Builder(string identifier, string title)
            {
                m_identifier    = identifier;
                m_title         = title;
            }
            
            /// <summary>
            /// Sets the subtitle of the app shortcut.
            /// </summary>
            /// <param name="subtitle"></param>
            /// <returns></returns>
            public Builder SetSubtitle(string subtitle)
            {
                m_subtitle = subtitle;
                return this;
            }
            
            /// <summary>
            /// Sets the icon file name of the app shortcut.
            /// </summary>
            /// <param name="iconFileNameWithExtension">Provide the icon file name with extension.</param>
            /// /note Make sure the icon is referred in the App Shortcuts Settings inspector.
            /// <returns></returns>
            public Builder SetIconFileName(string iconFileNameWithExtension)
            {
                m_iconFileName = iconFileNameWithExtension;
                return this;
            }

            /// <summary>
            /// Builds a new instance of the <see cref="AppShortcutItem"/> class.
            /// </summary>
            /// <returns></returns>
            public AppShortcutItem Build()
            {
                var item = new AppShortcutItem()
                {
                    Identifier = m_identifier,
                    Title = m_title
                };
                
                item.Subtitle = m_subtitle;
                
                if (!string.IsNullOrEmpty(m_iconFileName))
                {
                    item.IconFileName = m_iconFileName;
                }
                
                return item;
            }
        }
    }
}