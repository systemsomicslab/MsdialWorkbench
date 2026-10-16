namespace CompMs.App.Msdial.Model.Service
{
    /// <summary>
    /// How serious a <see cref="MessageRequest"/> is; the view shows it as the icon of the message.
    /// </summary>
    internal enum MessageSeverity
    {
        Information,
        Warning,
        Error,
    }

    /// <summary>
    /// A message that a model publishes on the message broker for the view to show, and, when it asks Yes or No, the answer.
    /// It carries no UI type, so a model can say something without referring to the view or the view models.
    /// </summary>
    internal sealed class MessageRequest
    {
        public string Caption { get; set; } = "Error";

        public string Content { get; set; } = string.Empty;

        public MessageSeverity Severity { get; set; } = MessageSeverity.Error;

        /// <summary>
        /// true to ask Yes or No; otherwise the message only has an OK button.
        /// </summary>
        public bool AsksYesNo { get; set; }

        /// <summary>
        /// true when the user answered Yes (or OK); null while nobody has answered.
        /// </summary>
        public bool? Result { get; set; }
    }
}
