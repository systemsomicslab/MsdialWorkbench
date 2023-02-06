//using java.awt;
//using java.awt.image;
//using java.io;
//using java.lang;
//using java.util;
//using javax.imageio;
//using org.openscience.cdk.interfaces;
//using org.openscience.cdk.layout;
//using org.openscience.cdk.renderer;
//using org.openscience.cdk.renderer.font;
//using org.openscience.cdk.renderer.generators;
//using org.openscience.cdk.renderer.visitor;
//using org.openscience.cdk.silent;
//using org.openscience.cdk.tools.manipulator;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Windows.Controls;

//namespace Riken.Metabolomics.StructureFinder.Utility
//{

//    //cite : https://gist.github.com/gilleain/1021964
//    //cite : https://searchcode.com/codesearch/view/6491/
//    public sealed class MoleculeImage
//    {
//        private MoleculeImage() { }

//        public static void TryClassLoad()
//        {
//            try {
//                SmilesToImage("C", 300, 300);
//            }
//            catch (ClassNotFoundException ex) {
//                ex.printStackTrace();
//            }
//        }

//        public static System.Drawing.Image SmilesToImage(string smiles, int width, int height)
//        {
//            if (width <= 0 || height <= 0) return null;

//            var errorString = string.Empty;
//            var iAtomContainer = MoleculeConverter.SmilesToIAtomContainer(smiles, out errorString);
//            if (iAtomContainer == null) return null;
//            return IAtomContainerToImage(iAtomContainer, width, height);
//        }

//        public static System.Windows.Media.Imaging.BitmapImage SmilesToMediaImageSource(string smiles, int width, int height)
//        {
//            if (width <= 0 || height <= 0) return null;

//            var errorString = string.Empty;
//            var iAtomContainer = MoleculeConverter.SmilesToIAtomContainer(smiles, out errorString);
//            if (iAtomContainer == null) return null;
//            var drawingImage = IAtomContainerToImage(iAtomContainer, width, height);


//            return ConvertDrawingImageToBitmap(drawingImage);
//        }

//        public static System.Drawing.Image IAtomContainerToImage(IAtomContainer iAtomContainer, int width, int height, bool isNumbered = false)
//        {
//            var errorString = string.Empty;

//            if (width <= 0 || height <= 0) return null;
//            if (iAtomContainer == null) return null;

//            IMolecule molecule = new Molecule(iAtomContainer);
//            //AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule);
            
//            var drawArea = new Rectangle(width, height);
//            var image = new BufferedImage(width, height, BufferedImage.TYPE_INT_RGB);

//            var sdg = new StructureDiagramGenerator();
//            sdg.setMolecule(molecule);
//            sdg.generateCoordinates();
//            molecule = sdg.getMolecule();

//            var generators = new ArrayList();
//            generators.add(new BasicSceneGenerator());
//            generators.add(new BasicBondGenerator());
//            generators.add(new BasicAtomGenerator());
//            if (isNumbered) generators.add(new AtomNumberGenerator());

//            var renderer = new AtomContainerRenderer(generators, new AWTFontManager());
//            renderer.setup(molecule, drawArea);
//            renderer.setScale(molecule);
            
//            var diagramBounds = renderer.calculateDiagramBounds((IAtomContainer)molecule);
//            renderer.setZoomToFit(drawArea.getWidth(), drawArea.getHeight(), diagramBounds.getWidth(), diagramBounds.getHeight());

//            var renderModel = renderer.getRenderer2DModel();
//            //renderModel.set(typeof(BasicAtomGenerator.ShowExplicitHydrogens), java.lang.Boolean.TRUE);
//            //for (int i = 0; i < iAtomContainer.getAtomCount(); i++) {
//            //    var iAtom = iAtomContainer.getAtom(i);
//            //    if (iAtom.getSymbol() != "C") {
//            //        Debug.WriteLine(iAtom.getSymbol());
//            //        //var connectedAtoms = iAtomContainer.getConnectedAtomsList(iAtom);
//            //        //if (connectedAtoms.size() == 1) {
//            //        //    var connectedAtom = (IAtom)connectedAtoms.get(0);
//            //        //    Debug.WriteLine(connectedAtom.getSymbol());
//            //        //}
//            //    }
//            //}

//            if (isNumbered) {
//                renderModel.set(typeof(AtomNumberGenerator.Offset), new javax.vecmath.Vector2d(0, 15));
//                renderModel.set(typeof(AtomNumberGenerator.WillDrawAtomNumbers), java.lang.Boolean.TRUE);
//                renderModel.set(typeof(AtomNumberGenerator.ColorByType), java.lang.Boolean.TRUE);
//            }
            
//            var g2 = (Graphics2D)image.getGraphics();
//            g2.setColor(Color.WHITE);
//            g2.fillRect(0, 0, width, height);
//            renderer.paint(molecule, new AWTDrawVisitor(g2));

//            var baos = new ByteArrayOutputStream();

//            try {
//                ImageIO.write((RenderedImage)image, "JPEG", baos);
//            }
//            catch (java.io.IOException e) {
//                e.printStackTrace();
//            }
//            finally {
//            }

//            if (baos == null) return null;

//            var sendData = baos.toByteArray();
//            return ByteArrayToImage(sendData);
//        }

//        public static void MoleculeImageInConsoleApp(IAtomContainer container, int width, int height, bool isNumbered)
//        {
//            var img = IAtomContainerToImage(container, width, height, isNumbered);
//            var bImg = ConvertDrawingImageToBitmap(img);
//            var wImg = new System.Windows.Controls.Image() { Source = bImg };
//            var window = new System.Windows.Window() {
//                Title = "Molecule image",
//                Width = 300,
//                Height = 300,
//                Content = new Grid {
//                    Children = {
//                        new StackPanel { Children= { wImg, }
//                                 }
//                        }
//                }
//            };

//            var app = new System.Windows.Application();
//            app.Run(window);
//        }

//        public static System.Windows.Media.Imaging.BitmapImage ConvertDrawingImageToBitmap(System.Drawing.Image image)
//        {
//            var ms = new MemoryStream();
//            image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

//            var bi = new System.Windows.Media.Imaging.BitmapImage();
//            bi.BeginInit();
//            bi.StreamSource = new MemoryStream(ms.ToArray());
//            bi.EndInit();

//            return bi;
//        }

//        public static System.Drawing.Image ByteArrayToImage(byte[] byteArrayIn)
//        {
//            var ms = new MemoryStream(byteArrayIn);
//            var returnImage = System.Drawing.Image.FromStream(ms);
//            return returnImage;
//        }
//    }
//}
