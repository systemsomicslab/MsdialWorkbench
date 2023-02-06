using System;

namespace edu.ucdavis.fiehnlab.MonaRestApi.exceptions {
	class MonaLoginException : Exception {
		public MonaLoginException(string msg = "Invalid Credentials") : base(msg) { }
	}
}
