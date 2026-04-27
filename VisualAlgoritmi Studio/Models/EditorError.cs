namespace VisualAlgoritmi_Studio.Models
{
    public sealed class EditorError
    {
        public string Code { get; }

        public string Message { get; }

        public int Line { get; }

        public int Column { get; }

        public EditorError(string code, string message, int line, int column)
        {
            Code = code;
            Message = message;
            Line = line;
            Column = column;
        }
    }
}