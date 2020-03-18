using NCDK.IO.Formats;
using NCDK.LibIO.CML;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace NCDK.IO
{
    /// <summary>
    /// Generates an RSS feed. It the object is a <see cref="IChemObjectSet{IAtomContainer}"/>, the molecules
    /// are put in separately. All other objects are made CML and put in.
    /// </summary>
    // @cdk.module       libiocml
    // @cdk.iooptions
    // @author Stefan Kuhn
    // @cdk.keyword RSS
    public class RssWriter : DefaultChemObjectWriter
    {
        private static readonly XNamespace NS_RSS10 = "http://purl.org/rss/1.0/";
        private static readonly XNamespace NS_RDF = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        private static readonly XNamespace NS_DCELEMENTS = "http://purl.org/dc/elements/1.1/";
        private static readonly XNamespace NS_CML = "http://www.xml-cml.org/schema/cml2/core/";

        private TextWriter writer;

        /// <summary>
        /// the link map. If you put a string in this map with one of the objects you want to write as key, it will be added as a link to this object (no validity check is done)
        /// </summary>
        public IReadOnlyDictionary<IChemObject, string> LinkMap { get; set; } = new Dictionary<IChemObject, string>();

        /// <summary>
        /// the date map. If you put a <see cref="DateTime"/> in this map with one of the objects you want to write as key, it will be added as a date to this object (no validity check is done)
        /// </summary>
        public IReadOnlyDictionary<IChemObject, DateTime> DateMap { get; set; } = new Dictionary<IChemObject, DateTime>();

        /// <summary>
        /// the title map. If you put a string in this map with one of the objects you want to write as key, it will be added as a title to this object (no validity check is done)
        /// </summary>
        public IReadOnlyDictionary<IChemObject, string> TitleMap { get; set; } = new Dictionary<IChemObject, string>();

        /// <summary>
        /// the creator map. If you put a string in this map with one of the objects you want to write as key, it will be added as a creator to this object (no validity check is done)
        /// </summary>
        public IReadOnlyDictionary<IChemObject, string> CreatorMap { get; set; } = new Dictionary<IChemObject, string>();

        /// <summary>
        /// InChI map If you put any number of strings in this map with one of the objects you want to write as key, it will be added as a child to the same node as the cml code of the object
        /// </summary>
        public IReadOnlyDictionary<IChemObject, string> InChIMap { get; set; } = new Dictionary<IChemObject, string>();

        public string Creator { get; set; } = "";

        /// <summary>
        /// This will be the title for the rss feed
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// This will be the link for the rss feed
        /// </summary>
        public string Link { get; set; } = "";

        /// <summary>
        /// This will be the description for the rss feed
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// This will be the Publisher for the rss feed
        /// </summary>
        public string Publisher { get; set; } = "";

        /// <summary>
        /// This will be the ImageLink for the rss feed
        /// </summary>
        public string ImageLink { get; set; } = "";

        /// <summary>
        /// This will be the About for the rss feed
        /// </summary>
        public string About { get; set; } = "";

        /// <summary>
        /// This will be added to the data as TimeZone. format according to 23c. Examples "+01:00" "-05:00"
        /// </summary>
        public string TimeZone { get; set; } = "+01:00";

        /// <summary>
        /// If you put any number of nu.xom.Elements in this map with one of the objects you want to write as key, it will be added as a child to the same node as the cml code of the object
        /// </summary>
        public IReadOnlyDictionary<IChemObject, IEnumerable<XElement>> MultiMap { get; set; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer.Dispose();
                }

                writer = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        public override IResourceFormat Format => CMLRSSFormat.Instance;

        public RssWriter(TextWriter output)
        {
            writer = output;
        }

        public RssWriter(Stream output)
            : this(new StreamWriter(output))
        { }

        public override bool Accepts(Type type) => true;

        /// <summary>
        /// Writes a <see cref="IChemObject"/> to the MDL molfile formated output.
        /// </summary>
        /// <param name="obj">Best choice is a set of molecules</param>
        /// <see cref="IChemObjectWriter.Write(IChemObject)"/>
        public override void Write(IChemObject obj)
        {
            try
            {
                XProcessingInstruction pi = new XProcessingInstruction("xml-stylesheet", "href=\"http://www.w3.org/2000/08/w3c-synd/style.css\" type=\"text/css\"");
                var doc = new XDocument();
                doc.Add(pi);
                var rdfElement = new XElement(NS_RSS10 + "RDF");
                rdfElement.SetAttributeValue(XNamespace.Xmlns + "rdf", NS_RSS10);
                rdfElement.SetAttributeValue(XNamespace.Xmlns + "mn", "http://usefulinc.com/rss/manifest/");
                rdfElement.SetAttributeValue(XNamespace.Xmlns + "dc", NS_DCELEMENTS);
                rdfElement.SetAttributeValue(XNamespace.Xmlns + "cml", Convertor.NamespaceCML);
                doc.Add(rdfElement);
                var channelElement = new XElement(NS_RSS10 + "channel");
                var titleElement = new XElement(NS_RSS10 + "title")
                {
                    Value = this.Title
                };
                channelElement.Add(titleElement);
                var linkElement = new XElement(NS_RSS10 + "link")
                {
                    Value = Link
                };
                channelElement.Add(linkElement);
                var descriptionElement = new XElement(NS_RSS10 + "description")
                {
                    Value = Description
                };
                channelElement.Add(descriptionElement);
                var publisherElement = new XElement(NS_DCELEMENTS + "publisher")
                {
                    Value = Publisher
                };
                channelElement.Add(publisherElement);
                var creatorElement = new XElement(NS_DCELEMENTS + "creator")
                {
                    Value = Creator
                };
                channelElement.Add(creatorElement);
                var imageElement = new XElement(NS_RSS10 + "image");
                imageElement.SetAttributeValue(NS_RDF + "resource", ImageLink);
                channelElement.Add(imageElement);
                var itemsElement = new XElement(NS_RSS10 + "items");
                var seqElement = new XElement(NS_RDF + "seq");
                itemsElement.Add(seqElement);
                channelElement.Add(itemsElement);
                channelElement.SetAttributeValue(NS_RDF + "about", About);
                rdfElement.Add(channelElement);
                var list = new List<IChemObject>();
                if (obj is IChemObjectSet<IAtomContainer>)
                {
                    for (int i = 0; i < ((IChemObjectSet<IAtomContainer>)obj).Count; i++)
                    {
                        list.Add(((IChemObjectSet<IAtomContainer>)obj)[i]);
                    }
                }
                else
                {
                    list.Add(obj);
                }
                for (int i = 0; i < list.Count; i++)
                {
                    IChemObject chemObject = (IChemObject)list[i];
                    var itemElement = new XElement(NS_RSS10 + "item");
                    string easylink = LinkMap[chemObject];
                    if (easylink != null) itemElement.SetAttributeValue(NS_RDF + "about", easylink);
                    var link2Element = new XElement(NS_RSS10 + "link")
                    {
                        Value = easylink
                    };
                    itemElement.Add(link2Element);
                    string title = chemObject.GetProperty<string>(CDKPropertyName.Title);
                    if (TitleMap[chemObject] != null)
                    {
                        var title2Element = new XElement(NS_RSS10 + "title")
                        {
                            Value = TitleMap[chemObject]
                        };
                        itemElement.Add(title2Element);
                    }
                    if (title != null)
                    {
                        var description2Element = new XElement(NS_RSS10 + "description")
                        {
                            Value = title
                        };
                        itemElement.Add(description2Element);
                        var subjectElement = new XElement(NS_DCELEMENTS + "subject")
                        {
                            Value = title
                        };
                        itemElement.Add(subjectElement);
                    }
                    if (DateMap[chemObject] != null)
                    {
                        var dateElement = new XElement(NS_DCELEMENTS + "date")
                        {
                            Value = DateMap[chemObject].ToString("yyyy-MM-dd'T'HH:mm:ss", DateTimeFormatInfo.InvariantInfo) + TimeZone
                        };
                        itemElement.Add(dateElement);
                    }
                    var creator2Element = new XElement(NS_DCELEMENTS + "creator")
                    {
                        Value = CreatorMap[chemObject]
                    };
                    itemElement.Add(creator2Element);
                    // add the InChI to the CMLRSS feed
                    if (InChIMap[chemObject] != null)
                    {
                        var inchiElement = new XElement(NS_CML + "identifier")
                        {
                            Value = InChIMap[chemObject]
                        };
                        itemElement.Add(inchiElement);
                    }
                    CMLElement root = null;
                    var convertor = new Convertor(true, null);
                    obj = list[i];
                    if (obj is ICrystal)
                    {
                        root = convertor.CDKCrystalToCMLMolecule((ICrystal)obj);
                    }
                    else if (obj is IAtomContainer)
                    {
                        root = convertor.CDKAtomContainerToCMLMolecule((IAtomContainer)obj);
                    }
                    else if (obj is IAtom)
                    {
                        root = convertor.CDKAtomToCMLAtom(null, (IAtom)obj);
                    }
                    else if (obj is IBond)
                    {
                        root = convertor.CDKJBondToCMLBond((IBond)obj);
                    }
                    else if (obj is IReaction)
                    {
                        root = convertor.CDKReactionToCMLReaction((IReaction)obj);
                    }
                    else if (obj is IReactionSet)
                    {
                        root = convertor.CDKReactionSetToCMLReactionList((IReactionSet)obj);
                    }
                    else if (obj is IChemObjectSet<IAtomContainer>)
                    {
                        root = convertor.CDKAtomContainerSetToCMLList((IChemObjectSet<IAtomContainer>)obj);
                    }
                    else if (obj is IChemSequence)
                    {
                        root = convertor.CDKChemSequenceToCMLList((IChemSequence)obj);
                    }
                    else if (obj is IChemModel)
                    {
                        root = convertor.CDKChemModelToCMLList((IChemModel)obj);
                    }
                    else if (obj is IChemFile)
                    {
                        root = convertor.CDKChemFileToCMLList((IChemFile)obj);
                    }
                    else
                    {
                        throw new CDKException($"Unsupported chemObject: {obj.GetType().Name}");
                    }
                    itemElement.Add(root);
                    if (MultiMap[chemObject] != null)
                    {
                        var coll = MultiMap[chemObject];
                        foreach (var e in coll)
                        {
                            itemElement.Add(e);
                        }
                    }
                    rdfElement.Add(itemElement);
                    var imageElement2 = new XElement(NS_RDF + "li");
                    imageElement2.SetAttributeValue(NS_RDF + "resource", LinkMap[chemObject]);
                    seqElement.Add(imageElement2);
                }
                writer.Write(doc.ToString());
                writer.Flush();
            }
            catch (IOException ex)
            {
                throw new CDKException(ex.Message, ex);
            }
        }
    }
}
