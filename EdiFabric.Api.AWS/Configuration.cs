namespace EdiFabric.Api.AWS
{
    public class Configuration
    {
        //  Change this to your serial. The free-plan serial is bd96a836feca45cb91c86ee65d281f52
        public static string ApiKey = "bd96a836feca45cb91c86ee65d281f52";
        //  Optional path to edifabric-x12-tools.dll/.so/.dylib, or leave empty to probe the output folder
        public static string LibraryPath = "";
        public static string ObjectName = "token";
        public static string BucketName = "edinationtestbucket";
    }
}
