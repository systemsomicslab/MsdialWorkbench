# Beam

This is based on snap shot of cdk-beam on [2019-04-24](https://github.com/johnmay/beam/commit/2def89e9f18724fbe1a599d1d1167109f5ec29c4 ).

[![DOI](https://zenodo.org/badge/12061606.svg)](https://zenodo.org/badge/latestdoi/12061606)

Beam - _to express by means of a radiant smile_ 

Beam is a free toolkit dedicated to parsing and generating Simplified
molecular-input line-entry system - [SMILES&trade;](http://en.wikipedia.org/wiki/Simplified_molecular-input_line-entry_system)
line notations. The primary focus of the library is to elegantly handle the
SMILES&trade; syntax and as fast as possible.

## Beaming

*Note: Beam is still in a development and some APIs will likely change until a release is made.*

One of the primary types in Beam is the `Graph` it provides convenience
methods for reading SMILES&trade; notation directly.

```cs
Graph g = Graph.FromSmiles("CCO");
```

and for writing it back to SMILES&trade; notation.

```cs
string smi = g.ToSmiles();
```

Beam provides excellent round tripping, preserving exactly how the input was
specified. Disregarding inputs with redundant brackets and erroneous/repeated
ring numbers - the actually input will generally be identical to the output.

```cs
// bond labels
Graph.FromSmiles("C1=CC=CC=C1").ToSmiles();    // kekule      (implicit single bonds)
Graph.FromSmiles("C-1=C-C=C-C=C1").ToSmiles(); // kekule      (explicit single bonds)
Graph.FromSmiles("c1ccccc1").ToSmiles();       // delocalised (implicit aromatic bonds)
Graph.FromSmiles("c:1:c:c:c:c:c1").ToSmiles(); // delocalised (explicit aromatic bonds)

// bracket atoms stay as bracket atoms
Graph.FromSmiles("[CH]1=[CH][CH]=[CH][CH]=[CH]1").ToSmiles();
Graph.FromSmiles("[CH]1=[CH]C=C[CH]=[CH]1").ToSmiles();       // mix bracket and subset atoms
```

Although preserving the representation was one of the design goals for beam it
is common to normalise output SMILES&trade;.

_Collapse_ a graph with labelled hydrogens `[CH3][CH2][OH]` to one with implicit
hydrogens `CCO`.

```cs
Graph g = Graph.FromSmiles("[CH3][CH2][OH]");
Graph h = Functions.Collapse(g);
h.ToSmiles().Equals("CCO");
```

_Expand_ a graph where the hydrogens are implicit `CCO` to one with labelled
hydrogens `[CH3][CH2][OH]`.

```cs
Graph g = Graph.FromSmiles("CCO");
Graph h = Functions.Expand(g);
h.ToSmiles().Equals("[CH3][CH2][OH]");
```

Stereo specification is persevered through rearrangements. The example below 
randomly generates arbitrary SMILES&trade; preserving correct stereo-configuration.

```cs
Graph g = Graph.FromSmiles("CCC[C@@](C)(O)[C@H](C)N");
StringBuilder sb = new StringBuilder(g.ToSmiles());
for (int i = 0; i < 25; i++)
    sb.Append('.').Append(Functions.Randomise(g).ToSmiles());
Console.WriteLine(sb);
```

Bond based double-bond configuration is normal in SMILES but can be problematic.
The issue is that a single symbol may be specifying two adjacent configurations.
A proposed extension was to use atom-based double-bond configuration.

Beam will input, output and convert atom and bond-based double-bond stereo 
specification. 

```cs
Graph  g   = Graph.FromSmiles("F/C=C/F");
Graph  h   = Functions.AtomBasedDBStereo(g);
string smi = h.ToSmiles();
smi.Equals("F[C@H]=[C@@H]F");
```

```cs
Graph  g   = Graph.FromSmiles("F[C@H]=[C@@H]F");
Graph  h   = Functions.BondBasedDBStereo(g);
string smi = h.ToSmiles();
smi.Equals("F/C=C/F");
```

Convert a graph with delocalised bonds to kekul&eacute; representation.

```cs
Graph  furan        = Graph.FromSmiles("o1cccc1");
Graph  furan_kekule = furan.Kekule();
string smi          = furan_kekule.ToSmiles();
smi.Equals("O1C=CC=C1");
```

With bond-based double-bond stereo specification there are two possible ways to
write each bond-based configuration. beam allows you to normalise the labels such
that the first symbol is always a forward slash (`/`). Some examples are shown
below.

```cs
Graph   g   = Graph.FromSmiles("F\\C=C/F");
Graph   h   = Functions.NormaliseDirectionalLabels(g);
string  smi = h.ToSmiles();
smi.Equals("F/C=C\\F");
```

```
F/C=C/C              is normalised to F/C=C/C
F\C=C\C              is normalised to F/C=C/C
F/C=C\C              is normalised to F/C=C\C
F\C=C/C              is normalised to F/C=C\C
C(\F)(/C)=C\C        is normalised to C(/F)(\C)=C/C
FC=C(F)C=C(F)\C=C\C  is normalised to FC=C(F)C=C(F)/C=C/C
```

## Beam me up

beam is still in development but you can obtain the latest build from the [EBI snapshots repository](http://www.ebi.ac.uk/intact/maven/nexus/content/repositories/ebi-repo-snapshots/). An example configuration for maven is shown below.

```xml
<project>
...
<repositories>
   <repository>
      <id>ebi-repo</id>
      <url>http://www.ebi.ac.uk/intact/maven/nexus/content/repositories/ebi-repo/</url>
   </repository>
   <repository>
      <id>ebi-repo-snapshots</id>
      <url>http://www.ebi.ac.uk/intact/maven/nexus/content/repositories/ebi-repo-snapshots/</url>
   </repository>
</repositories>
...
<dependencies>
    <dependency>
        <groupId>uk.ac.ebi.beam</groupId>
        <artifactId>beam-core</artifactId>
        <version>LATEST</version>
    </dependency>
    <dependency>
        <groupId>uk.ac.ebi.beam</groupId>
        <artifactId>beam-func</artifactId>
        <version>LATEST</version>
    </dependency>
</dependencies>
...
</project>
```

## License BSD 2-Clause

Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those of the authors and should not be interpreted as representing official policies, either expressed or implied, of the FreeBSD Project.

## How to cite

Use the DOI at the top of this README or: 
  John Mayfield, BEAM v1.0, 2017, www.github.com/johnmay/beam

---------------------------------------

&trade;: SMILES is a trademark of [Daylight Chemical Information Systems](http://daylight.com/)
