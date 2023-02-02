using NCDK;
using NCDK.Depict;
using NCDK.FaulonSignatures.Chemistry;
using NCDK.Graphs;
using NCDK.Layout;
using NCDK.Renderers;
using NCDK.Renderers.Fonts;
using NCDK.Renderers.Generators;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Riken.Metabolomics.StructureFinder.Utility
{

    //cite : https://gist.github.com/gilleain/1021964
    //cite : https://searchcode.com/codesearch/view/6491/
    public sealed class MoleculeImage
    {
        private static readonly IChemObjectBuilder builder = CDK.Builder;
        private static StructureDiagramGenerator sdg = new StructureDiagramGenerator();
        private static SmilesParser parser = new SmilesParser(builder);
        private static SmilesGenerator smilesGenerator = new SmilesGenerator(SmiFlavors.Canonical);

        private MoleculeImage() { }

        //public static void TryClassLoad()
        //{
        //    try {
        //        SmilesToImage("C", 300, 300);
        //    }
        //    catch (CDKException ex) {
        //        Console.WriteLine(ex.ToString());
        //    }
        //}

        public static IAtomContainer SmilesToIAtomContainer(string smiles, out string error) {
            error = "Error\r\n";

            IAtomContainer container = null;
            try {
                container = parser.ParseSmiles(smiles);
            }
            catch (InvalidSmilesException) {
                error += "SMILES: cannot be converted.\r\n";
                return null;
            }

            if (!ConnectivityChecker.IsConnected(container)) {
                error += "SMILES: the connectivity is not correct.\r\n";
                return null;
            }

            return container;
        }

        //public static System.Drawing.Image SmilesToImage(string smiles, int width, int height)
        //{
        //    if (width <= 0 || height <= 0) return null;

        //    var errorString = string.Empty;
        //    var iAtomContainer = SmilesToIAtomContainer(smiles, out errorString);
        //    if (iAtomContainer == null) return null;
        //    return IAtomContainerToImage(iAtomContainer, width, height);
        //}

        public static BitmapImage SmilesToMediaImageSource(string smiles, int width, int height)
        {
            if (width <= 0 || height <= 0) return null;

            var errorString = string.Empty;
            var iAtomContainer = SmilesToIAtomContainer(smiles, out errorString);
            if (iAtomContainer == null) return null;

            var depiction = new DepictionGenerator().Depict(iAtomContainer);
            var bitMap = depiction.ToBitmap(width, height, PixelFormats.Pbgra32);
            var image = ConvertRenderTargetBitmapToBitmapImage(bitMap);
            return image;

            //var drawingImage = IAtomContainerToImage(iAtomContainer, width, height);
            //return image.bi;


            //return ConvertDrawingImageToBitmap(drawingImage);
        }

        public static BitmapImage ConvertRenderTargetBitmapToBitmapImage(RenderTargetBitmap renderTargetBitmap) {

            //var bitmapSource = (BitmapSource)renderTargetBitmap;
            //return (BitmapImage)bitmapSource;

            var bitmapImage = new BitmapImage();
            var bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            using (var stream = new MemoryStream()) {
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            
            return bitmapImage;
        }

        //public static System.Drawing.Image IAtomContainerToImage(IAtomContainer iAtomContainer, int width, int height, bool isNumbered = false)
        //{
        //    var depiction = new DepictionGenerator().Depict(iAtomContainer);
        //    var image = depiction.ToBitmap(width, height, new PixelFormat());

        //   // return null;
        //    //var errorString = string.Empty;

        //    //if (width <= 0 || height <= 0) return null;
        //    //if (iAtomContainer == null) return null;

        //    //IMolecule molecule = new Molecule(iAtomContainer);
        //    ////AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule);

        //    //var drawArea = new Rectangle(width, height);
        //    //var image = new BufferedImage(width, height, BufferedImage.TYPE_INT_RGB);

        //    //var sdg = new StructureDiagramGenerator();
        //    //sdg.setMolecule(molecule);
        //    //sdg.generateCoordinates();
        //    //molecule = sdg.getMolecule();

        //    //var generators = new List<IGenerator<IAtomContainer>>();
        //    //generators.Add(new BasicSceneGenerator());
        //    //generators.Add(new BasicBondGenerator());
        //    //generators.Add(new BasicAtomGenerator());
        //    //if (isNumbered) generators.Add(new AtomNumberGenerator());

        //    //var renderer = new AtomContainerRenderer(generators, IFontManager);
        //    //renderer.setup(molecule, drawArea);
        //    //renderer.setScale(molecule);

        //    //var diagramBounds = renderer.calculateDiagramBounds((IAtomContainer)molecule);
        //    //renderer.setZoomToFit(drawArea.getWidth(), drawArea.getHeight(), diagramBounds.getWidth(), diagramBounds.getHeight());

        //    //var renderModel = renderer.getRenderer2DModel();
        //    //renderModel.set(typeof(BasicAtomGenerator.ShowExplicitHydrogens), java.lang.Boolean.TRUE);
        //    //for (int i = 0; i < iAtomContainer.getAtomCount(); i++) {
        //    //    var iAtom = iAtomContainer.getAtom(i);
        //    //    if (iAtom.getSymbol() != "C") {
        //    //        Debug.WriteLine(iAtom.getSymbol());
        //    //        //var connectedAtoms = iAtomContainer.getConnectedAtomsList(iAtom);
        //    //        //if (connectedAtoms.size() == 1) {
        //    //        //    var connectedAtom = (IAtom)connectedAtoms.get(0);
        //    //        //    Debug.WriteLine(connectedAtom.getSymbol());
        //    //        //}
        //    //    }
        //    //}

        //    //if (isNumbered) {
        //    //    renderModel.set(typeof(AtomNumberGenerator.Offset), new javax.vecmath.Vector2d(0, 15));
        //    //    renderModel.set(typeof(AtomNumberGenerator.WillDrawAtomNumbers), java.lang.Boolean.TRUE);
        //    //    renderModel.set(typeof(AtomNumberGenerator.ColorByType), java.lang.Boolean.TRUE);
        //    //}

        //    //var g2 = (Graphics2D)image.getGraphics();
        //    //g2.setColor(Color.WHITE);
        //    //g2.fillRect(0, 0, width, height);
        //    //renderer.paint(molecule, new AWTDrawVisitor(g2));

        //    //var baos = new ByteArrayOutputStream();

        //    //try {
        //    //    ImageIO.write((RenderedImage)image, "JPEG", baos);
        //    //}
        //    //catch (java.io.IOException e) {
        //    //    e.printStackTrace();
        //    //}
        //    //finally {
        //    //}

        //    //if (baos == null) return null;

        //    //var sendData = baos.toByteArray();
        //    //return ByteArrayToImage(sendData);
        //}

        //public static void MoleculeImageInConsoleApp(IAtomContainer container, int width, int height, bool isNumbered)
        //{
        //    var img = IAtomContainerToImage(container, width, height, isNumbered);
        //    var bImg = ConvertDrawingImageToBitmap(img);
        //    var wImg = new System.Windows.Controls.Image() { Source = bImg };
        //    var window = new System.Windows.Window() {
        //        Title = "Molecule image",
        //        Width = 300,
        //        Height = 300,
        //        Content = new Grid {
        //            Children = {
        //                new StackPanel { Children= { wImg, }
        //                         }
        //                }
        //        }
        //    };

        //    var app = new System.Windows.Application();
        //    app.Run(window);
        //}

        public static System.Windows.Media.Imaging.BitmapImage ConvertDrawingImageToBitmap(System.Drawing.Image image)
        {
            var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            var bi = new System.Windows.Media.Imaging.BitmapImage();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(ms.ToArray());
            bi.EndInit();

            return bi;
        }

        public static System.Drawing.Image ByteArrayToImage(byte[] byteArrayIn)
        {
            var ms = new MemoryStream(byteArrayIn);
            var returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }
    }
}
