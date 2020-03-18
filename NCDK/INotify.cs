using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCDK
{
    public interface INotify
    {

        /// <summary>
        /// <see cref="IChemObjectListener"/>s of this <see cref="IChemObject"/>.
        /// </summary>
        ICollection<IChemObjectListener> Listeners { get; }

        /// <summary>
        /// The flag that indicates whether notification messages are sent around.
        /// </summary>
        bool Notification { get; set; }

        /// <summary>
        /// This should be triggered by an method that changes the content of an object
        /// to that the registered listeners can react to it.
        /// </summary>
        void NotifyChanged();
    }
}
