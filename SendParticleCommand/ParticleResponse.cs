using System.Runtime.Serialization;

namespace SendParticleCommand
{
    [DataContract]
    public class ParticleResponse
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string last_app { get; set; }
        [DataMember]
        public bool connected { get; set; }
        [DataMember]
        public int return_value { get; set; }
    }

}