namespace api.nox.network.Auths
{
    [System.Serializable]
    public class IntegrityRequest
    {
        public string address;
        
        public override string ToString()
            => $"{GetType().Name}[address={address}]";
    }
}