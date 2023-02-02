namespace NCDK.FaulonSignatures.Simple
{
    public class SimpleGraphFactory
    {
        public static SimpleGraph Make4Cube()
        {
            SimpleGraph g = new SimpleGraph();
            g.MakeEdges(0, 1, 7, 9, 15);
            g.MakeEdges(1, 2, 8, 10);
            g.MakeEdges(2, 3, 9, 11);
            g.MakeEdges(3, 4, 10, 12);
            g.MakeEdges(4, 5, 11, 13);
            g.MakeEdges(5, 6, 12, 14);
            g.MakeEdges(6, 7, 13, 15);
            g.MakeEdges(7, 8, 14);
            g.MakeEdges(8, 11, 13);
            g.MakeEdges(9, 12, 14);
            g.MakeEdges(10, 13, 15);
            g.MakeEdges(11, 14);
            g.MakeEdges(12, 15);
            return g;
        }

        public static SimpleGraph MakeHerschelGraph()
        {
            SimpleGraph g = new SimpleGraph();
            g.MakeEdge(0, 1);
            g.MakeEdge(0, 3);
            g.MakeEdge(0, 5);
            g.MakeEdge(1, 2);
            g.MakeEdge(1, 6);
            g.MakeEdge(2, 3);
            g.MakeEdge(2, 7);
            g.MakeEdge(2, 8);
            g.MakeEdge(3, 4);
            g.MakeEdge(4, 5);
            g.MakeEdge(4, 8);
            g.MakeEdge(4, 10);
            g.MakeEdge(5, 6);
            g.MakeEdge(6, 7);
            g.MakeEdge(6, 10);
            g.MakeEdge(7, 9);
            g.MakeEdge(8, 9);
            g.MakeEdge(9, 10);
            return g;
        }

        public static SimpleGraph MakeGrotschGraph()
        {
            SimpleGraph g = new SimpleGraph();
            g.MakeEdge(0, 1);
            g.MakeEdge(0, 2);
            g.MakeEdge(0, 3);
            g.MakeEdge(0, 4);
            g.MakeEdge(0, 5);
            g.MakeEdge(1, 7);
            g.MakeEdge(1, 10);
            g.MakeEdge(2, 6);
            g.MakeEdge(2, 8);
            g.MakeEdge(3, 7);
            g.MakeEdge(3, 9);
            g.MakeEdge(4, 8);
            g.MakeEdge(4, 10);
            g.MakeEdge(5, 6);
            g.MakeEdge(5, 9);
            g.MakeEdge(6, 7);
            g.MakeEdge(6, 10);
            g.MakeEdge(7, 8);
            g.MakeEdge(8, 9);
            g.MakeEdge(9, 10);
            return g;
        }

        public static SimpleGraph MakeQuadricyclane()
        {
            SimpleGraph g = new SimpleGraph();
            g.MakeEdge(0, 1);
            g.MakeEdge(0, 5);
            g.MakeEdge(0, 6);
            g.MakeEdge(1, 2);
            g.MakeEdge(2, 3);
            g.MakeEdge(2, 4);
            g.MakeEdge(3, 4);
            g.MakeEdge(3, 6);
            g.MakeEdge(4, 5);
            g.MakeEdge(5, 6);
            return g;
        }

        public static SimpleGraph MakeFourRegularExample()
        {
            SimpleGraph g = new SimpleGraph();
            g.MakeEdge(0, 1);
            g.MakeEdge(0, 3);
            g.MakeEdge(0, 4);
            g.MakeEdge(0, 5);
            g.MakeEdge(1, 2);
            g.MakeEdge(1, 6);
            g.MakeEdge(1, 7);
            g.MakeEdge(2, 3);
            g.MakeEdge(2, 6);
            g.MakeEdge(2, 7);
            g.MakeEdge(3, 4);
            g.MakeEdge(3, 5);
            g.MakeEdge(4, 5);
            g.MakeEdge(4, 6);
            g.MakeEdge(5, 7);
            g.MakeEdge(6, 7);
            return g;
        }

        public static SimpleGraph MakeThreeFourFiveTwisted()
        {
            SimpleGraph g = new SimpleGraph();
            g.MakeEdge(0, 1);
            g.MakeEdge(0, 2);
            g.MakeEdge(0, 7);
            g.MakeEdge(1, 2);
            g.MakeEdge(1, 3);
            g.MakeEdge(2, 5);
            g.MakeEdge(3, 4);
            g.MakeEdge(3, 6);
            g.MakeEdge(4, 5);
            g.MakeEdge(4, 7);
            g.MakeEdge(5, 6);
            g.MakeEdge(6, 7);
            return g;
        }

        public static SimpleGraph MakeSpiroPentagons()
        {
            SimpleGraph g = new SimpleGraph();
            g.MakeEdge(0, 1);
            g.MakeEdge(0, 2);
            g.MakeEdge(0, 3);
            g.MakeEdge(0, 4);
            g.MakeEdge(1, 5);
            g.MakeEdge(2, 6);
            g.MakeEdge(3, 7);
            g.MakeEdge(4, 8);
            g.MakeEdge(5, 6);
            g.MakeEdge(7, 8);
            return g;
        }

        public static SimpleGraph MakePrism(int size)
        {
            SimpleGraph g = new SimpleGraph();
            for (int i = 0; i < size - 1; i++)
            {
                g.MakeEdge(i, i + 1);
            }
            g.MakeEdge(size - 1, 0);
            for (int i = 0; i < size - 1; i++)
            {
                g.MakeEdge(i + size, i + size + 1);
            }
            g.MakeEdge((2 * size) - 1, size);
            for (int i = 0; i < size; i++)
            {
                g.MakeEdge(i, i + size);
            }
            return g;
        }

        public static SimpleGraph MakeSandwich(int size)
        {
            SimpleGraph g = new SimpleGraph();
            int center = size * 2;
            // face A
            for (int i = 0; i < size - 1; i++)
            {
                g.MakeEdge(i, i + 1);
                g.MakeEdge(i, center);
            }
            g.MakeEdge(size - 1, 0);
            g.MakeEdge(size - 1, center);

            // face B
            for (int i = 0; i < size - 1; i++)
            {
                g.MakeEdge(i + size, i + size + 1);
                g.MakeEdge(i + size, center);
            }
            g.MakeEdge((2 * size) - 1, size);
            g.MakeEdge((2 * size) - 1, center);

            return g;
        }

        public static SimpleGraph MakePetersensGraph()
        {
            return new SimpleGraph(
            "0:1,0:5,0:4,1:2,1:6,2:3,2:7,3:8,3:4,4:9,5:7,5:8,6:8,6:9,7:9");
        }

        public static SimpleGraph MakePappusGraph()
        {
            return new SimpleGraph(
                    "0:1,0:5,0:17,1:2,1:8,2:3,2:13,3:4,3:10,4:5,4:15,5:6,6:7," +
                    "6:11,7:8,7:14,8:9,9:10,9:16,10:11,11:12,12:13,12:17,13:14," +
                    "14:15,15:16,16:17"
            );
        }

        public static SimpleGraph MakeTietzesGraph()
        {
            return new SimpleGraph(
                    "0:1,0:4,0:8,1:2,1:6,2:3,2:10,3:4,3:7,4:5," +
            "5:6,5:11,6:7,7:8,8:9,9:10,9:11,10:11");

        }

        public static SimpleGraph MakeTruncatedTetrahedron()
        {
            return
            new SimpleGraph("0:1,0:2,0:3,1:2,1:4,2:5,3:6,3:7,4:8,4:9,5:10,5:11,"
                    + "6:7,6:8,7:11,8:9,9:10,10:11");
        }

        public static SimpleGraph MakeDoubleBridgedPentagon()
        {
            return new SimpleGraph("0:1,0:2,1:3,1:5,1:6,2:4,2:5,2:6,3:4,3:5,4:6");
        }

        public static SimpleGraph MakeAdamantane()
        {
            return new SimpleGraph(
            "0:1,0:2,0:3,1:4,2:5,3:6,4:7,4:9,5:7,5:8,6:8,6:9");
        }

        public static SimpleGraph MakeDiamantane()
        {
            return new SimpleGraph(
            "0:1,0:5,1:2,1:12,2:3,2:13,3:4,3:8,4:5,5:6,6:7,7:8,7:12,8:9,9:10,10:11,10:13,11:12");
        }

        public static SimpleGraph MakeCuneane()
        {
            return new SimpleGraph(
            "0:1,0:3,0:5,1:2,1:7,2:3,2:7,3:4,4:5,4:6,5:6,6:7");
        }

        public static SimpleGraph MakeTwistane()
        {
            return new SimpleGraph("0:1,0:2,0:3,1:4,1:5,2:6,3:7,4:8,5:9,6:8,6:9,7:9");
        }

        public static SimpleGraph MakeSymmetric1Twistane()
        {
            return new SimpleGraph("0:1,0:7,1:2,1:5,2:3,3:4,3:7,4:5,5:6,6:7");
        }

        public static SimpleGraph MakeSymmetric2Twistane()
        {
            return new SimpleGraph(
                    "0:1,0:11,1:2,2:3,2:8,3:4,4:5,5:6,5:11,6:7,7:8,8:9,9:10,10:11");
        }

        public static SimpleGraph MakeNapthalene()
        {
            return new SimpleGraph("0:1,0:5,1:2,1:6,2:3,2:9,3:4,4:5,6:7,7:8,8:9");
        }

        public static SimpleGraph MakeSquareQuotientGraph()
        {
            // a very odd graph designed to have a square quotient graph
            return
            new SimpleGraph(
            "0:1,0:2,0:9,1:3,1:4,1:5,2:6,2:7,2:8,3:9,4:9,5:9,6:9,7:9,8:9");
        }

        public static SimpleGraph MakeBowtieane()
        {
            // don't know the name of the structure
            return new SimpleGraph(
            "0:1,0:7,1:2,1:8,2:3,2:6,3:4,3:9,4:5,5:6,5:9,6:7,7:8,8:9");

            // XXX bug with below (3-regular) structure:
            // addition of a bond between 0 and 4 breaks the symmetry of two of
            // the vertices :( FIXME 
            //            "0:1,0:4,0:7,1:2,1:8,2:3,2:6,3:4,3:9,4:5,5:6,5:9,6:7,7:8,8:9");
        }

        public static SimpleGraph MakeDiSpiroOctane()
        {
            return new SimpleGraph("0:1,0:2,1:2,1:3,1:4,2:3,2:6,3:5,3:7,4:5,6:7");
        }

        public static SimpleGraph Make26Fullerene()
        {
            return new SimpleGraph(
                    "0:1,0:4,0:5,1:2,1:7,2:3,2:9,3:4,3:12,4:14,5:6,5:15,6:7,6:17," +
                    "7:8,8:9,8:19,9:10,10:11,10:20,11:12,11:22,12:13,13:14,13:23," +
                    "14:15,15:16,16:17,16:24,17:18,18:19,18:25,19:20,20:21,21:22," +
                    "21:25,22:23,23:24,24:25"
            );
        }

        public static SimpleGraph MakeTricycloPropaIndene()
        {
            return new SimpleGraph(
            "0:1,0:2,1:2,1:3,2:4,3:5,3:7,4:6,4:8,5:6,5:7,6:8,7:9,8:9");
        }
    }
}
