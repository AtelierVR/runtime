namespace api.nox.discord
{
    public class ReadyMessage
    {
        private readonly object _message;

        public ReadyMessage(object msg)
        {
            _message = msg;
        }
    }
}