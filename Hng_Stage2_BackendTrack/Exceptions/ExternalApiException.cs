namespace Hng_Stage2_BackendTrack.Exceptions
{
    public class ExternalApiException : Exception
    {
        public string ApiName { get; }
        public ExternalApiException(string apiName, string message) : base(message) => ApiName = apiName;
    }

}
