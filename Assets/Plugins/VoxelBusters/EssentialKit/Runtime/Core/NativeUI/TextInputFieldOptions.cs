namespace VoxelBusters.EssentialKit
{
    public class TextInputFieldOptions
    {
        public string Text { get; private set; }
        public string PlaceholderText { get; private set; }
        public bool IsSecured { get; private set; }
        public KeyboardInputType KeyboardInputType { get; private set; }
        

        public class Builder
        {
            private TextInputFieldOptions m_options = new TextInputFieldOptions();
            
            public Builder SetText(string text)
            {
                m_options.Text = text;
                return this;
            }
            public Builder SetPlaceholderText(string placeholderText)
            {
                m_options.PlaceholderText = placeholderText;
                return this;
            }
            
            public Builder SetIsSecured(bool isSecured)
            {
                m_options.IsSecured = isSecured;
                return this;
            }

            public Builder SetKeyboardInputType(KeyboardInputType type)
            {
                m_options.KeyboardInputType = type;
                return this;
            }
            
            public TextInputFieldOptions Build()
            {
                return m_options;
            }
        }
    }
}