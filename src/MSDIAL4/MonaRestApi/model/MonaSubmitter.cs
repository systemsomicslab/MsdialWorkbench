using edu.ucdavis.fiehnlab.MonaRestApi.mvvm;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
    [DataContract]
    public class MonaSubmitter : ObservableObject {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string firstName { get; set; }
        [DataMember]
        public string lastName { get; set; }
        [DataMember]
        public string emailAddress { get; set; }
        [DataMember]
        public string institution { get; set; }

        [IgnoreDataMember]
        public string FullName {
            get { return firstName + " " + lastName; }
            private set { }
        }

        public override string ToString() {
            return string.Format("Id: {0}, Full Name: {1} {2}, e-mail: {3}, Institution: {4}", 
                id, firstName, lastName, emailAddress, institution);
        }

        public Submitter asSubmitter() {
            return new Submitter() { firstName = firstName, lastName = lastName, emailAddress = emailAddress, institution = institution };
        }
    }
}
