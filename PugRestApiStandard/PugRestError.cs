using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class PugRestError
    {
        public static Dictionary<int, string> StatusCodesDict = new Dictionary<int, string>
        {
            {200, "PUGREST.OK: Success"},
            {400, "PUGREST.BadRequest: Request is improperly formed (syntax error in the URL, POST body, etc.)"},
            {404, "PUGREST.NotFound: The input record was not found (e.g. invalid CID)"},
            {405, "PUGREST.MethodNotAllowed: Request not allowed (such as invalid MIME type in the HTTP Accept header)"},
            {429, "ProtocolError: The remote server returned an error: (429) Too Many Requests."},
            {500, "PUGREST.ServerError: Some problem on the server side (such as a database server down, etc.)"},
            {501, "PUGREST.Timeout: The requested operation has not (yet) been implemented by the server"},
            {504, "PUGREST.Timeout: The request timed out, from server overload or too broad a request"}
        };
    }
}
