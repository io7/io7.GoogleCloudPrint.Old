using System.Runtime.Serialization;

namespace GoogleCloudPrint.Model
{
    [DataContract]
    public class CloudPrintShare
    {
        [DataMember]
        public bool success { get; set; }

        [DataMember]
        public string message { get; set; }

    }
}
