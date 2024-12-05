The following software might be used in this product:

# The Accord.NET Framework
=====================================================
Copyright (c) 2009-2017, Accord.NET Authors <authors @ accord-framework.net>

    This library is free software; you can redistribute it and/or modify it under the terms of
    the GNU Lesser General Public License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    The copyright holders provide no reassurances that the source code provided does
    not infringe any patent, copyright, or any other intellectual property rights of
    third parties. The copyright holders disclaim any liability to any recipient for
    claims brought against recipient by any third party for infringement of that parties
    intellectual property rights.

    This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
    without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.


Some modules of this library are distributed under the GPL, and not the LGPL. Those modules
have been marked with .GPL.dll in their name. All relevant licensing information should also
be accompanying each respective source code. Other extra modules may carry other restrictions.
Please see relevant source headers for details.


The documentation sections contains references to Wikipedia and is thus entirely available under the
Creative Commons Attribution/Share-Alike License. All referenced sections include a hyperlink to the
original article page. If you think you have found something which has not been properly linked, please
let me know so it can be corrected.

Overall framework architecture and style was greatly inspired by AForge.NET. In May 2015, this project
has been merged with the AForge.NET framework since public support for AForge.NET has ended. The original
AForge.NET Framework is a copyrighted work by Andrew Kirillov, altough developed and shared under the same
LGPL license.



This library also contains routines adapted from Lutz Roeder's Mapack for .NET, LAPACK and Jama
routines. Jama has been released under the public domain. LAPACK has been released under a compatible
3-clause BSD license, listed below. Original ArrayDataView code is copyright (c) 2004 Mihail Stefanov,
licensed under the LGPL with permission of the original author. Original SortableBindingList source
code is copyright (c) 2008 Tim Van Wassenhove, licensed under the LGPL with permission of the original
author. Statistics module contains code for computing the Kolmogorov-Smirnov distribution based on
original code by Richard Simard, redistributed under the LGPL with permission of the original author.
Imaging module contains code for Gabor filter based on the original code by Max Bügler, redistributed
under the LGPL with permission of the original author. 

The Augmented Lagrangian Solver implementation is based on the NLopt Numerical Optimization Library, 
copyright (c) 2008 Steven G. Johnson, distributed under the MIT license. Some image filters have been
contributed by Diego Catalano, distributed under the LGPL license and included with permission of the
original author. Mathematics module includes the Kinematic joint models by Rémy Dispagne, licensed
under the LGPL with permission of the original author. The Haar Object detector contains contributions
from Darko Jurić, added with permission of the original author.

The Accord.Vision module includes definitions for Haar Cascades created by Modesto Castrillon-Santana,
distributed under the LGPL with permission of the original author. The SURF interest points detector
implementation is based on code by Christopher Evans, licensed under the LGPL with permission of the
original author. The FAST corners detector has been created using a code generation tool created by
Edward Rosten, distributed under a compatible 3-clause BSD license, listed below. The Camshift tracker
implementation is based on the FaceIt ActionScript Camshift tracker, copyright (c) 2009 Benjamin Jung,
distributed under the MIT license, listed below. The Project Marilena is copyright (c) 2008 Masakazu
Ohtsuka, distributed under a compatible 2-clause BSD license, listed below. See actual sources for
details. 



Below are the licenses applying to specific sections of the code. Those are not
applicable to the work as a whole (the Framework), but to the specifics of each
portion of the framework. See actual sources for details.


The FaceIt Library - Copyright (C) 2009 Benjamin Jung

     Permission is hereby granted, free of charge, to any person obtaining a copy
     of this software and associated documentation files (the "Software"), to deal
     in the Software without restriction, including without limitation the rights
     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
     copies of the Software, and to permit persons to whom the Software is
     furnished to do so, subject to the following conditions:

     The above copyright notice and this permission notice shall be included in
     all copies or substantial portions of the Software.

     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
     THE SOFTWARE.


The Marilena Project - Copyright (C) 2008 Masakazu Ohtsuka

     Redistribution and use in source and binary forms, with or without modification,
     are permitted provided that the following conditions are met:

       * Redistribution's of source code must retain the above copyright notice,
         this list of conditions and the following disclaimer.

       * Redistribution's in binary form must reproduce the above copyright notice,
         this list of conditions and the following disclaimer in the documentation
         and/or other materials provided with the distribution.

     This software is provided by the copyright holders and contributors "as is" and
     any express or implied warranties, including, but not limited to, the implied
     warranties of merchantability and fitness for a particular purpose are disclaimed.
     In no event shall the Intel Corporation or contributors be liable for any direct,
     indirect, incidental, special, exemplary, or consequential damages
     (including, but not limited to, procurement of substitute goods or services;
     loss of use, data, or profits; or business interruption) however caused
     and on any theory of liability, whether in contract, strict liability,
     or tort (including negligence or otherwise) arising in any way out of
     the use of this software, even if advised of the possibility of such damage.


The FAST corners detector - Copyright (C) 2006-2010 Edward Rosten

     Redistribution and use in source and binary forms, with or without
     modification, are permitted provided that the following conditions
     are met:

       * Redistributions of source code must retain the above copyright
         notice, this list of conditions and the following disclaimer.

       * Redistributions in binary form must reproduce the above copyright
         notice, this list of conditions and the following disclaimer in the
         documentation and/or other materials provided with the distribution.

       * Neither the name of the University of Cambridge nor the names of 
         its contributors may be used to endorse or promote products derived 
         from this software without specific prior written permission.

     THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
     "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
     LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
     A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
     CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
     EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
     PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
     PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
     LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
     NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
     SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


The Cephes Math Library - Copyright (C) 1984-2000 Stephen L. Moshier

     Release 2.8 - June, 2000 Copyright 1984, 1987, 1988, 2000 by Stephen L. Moshier

       Some software in this archive may be from the book _Methods and
     Programs for Mathematical Functions_ (Prentice-Hall or Simon & Schuster
     International, 1989) or from the Cephes Mathematical Library, a
     commercial product. In either event, it is copyrighted by the author.
     What you see here may be used freely but it comes with no support or
     guarantee.

       The two known misprints in the book are repaired here in the
     source listings for the gamma function and the incomplete beta
     integral.


       Stephen L. Moshier
       moshier@na-net.ornl.gov


The LAPACK Linear Algebra Package - Copyright (C) 1992-2010

     Copyright (c) 1992-2010 The University of Tennessee and The University of Tennessee Research Foundation.  All rights reserved.
     Copyright (c) 2000-2010 The University of California Berkeley. All rights reserved.
     Copyright (c) 2006-2010 The University of Colorado Denver. All rights reserved.

     Redistribution and use in source and binary forms, with or without
     modification, are permitted provided that the following conditions are
     met:

     - Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer.
 
     - Redistributions in binary form must reproduce the above copyright
       notice, this list of conditions and the following disclaimer listed
       in this license in the documentation and/or other materials
       provided with the distribution.

     - Neither the name of the copyright holders nor the names of its
       contributors may be used to endorse or promote products derived from
       this software without specific prior written permission.

     The copyright holders provide no reassurances that the source code
     provided does not infringe any patent, copyright, or any other
     intellectual property rights of third parties.  The copyright holders
     disclaim any liability to any recipient for claims brought against
     recipient by any third party for infringement of that parties
     intellectual property rights.

     THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
     "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
     LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
     A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
     OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
     SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
     LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
     DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
     THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
     (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
     OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


NLopt Numerical Optimization Library - Copyright (c) 2008-2014 Steven G. Johnson

    Copyright (c) 2007-2011 Massachusetts Institute of Technology

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:
  
    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.
  
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 


 Peter Kovesi's Computer Vision functions - Copyright (c) 1995-2010 Peter Kovesi

    Copyright (c) 1995-2010 Peter Kovesi
    Centre for Exploration Targeting
    School of Earth and Environment
    The University of Western Australia

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights 
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
    of the Software, and to permit persons to whom the Software is furnished to do
    so, subject to the following conditions:
   
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
   
    The software is provided "as is", without warranty of any kind, express or
    implied, including but not limited to the warranties of merchantability, 
    fitness for a particular purpose and noninfringement. In no event shall the
    authors or copyright holders be liable for any claim, damages or other liability,
    whether in an action of contract, tort or otherwise, arising from, out of or in
    connection with the software or the use or other dealings in the software.


 LibSVM & liblinear library for Support Vector Machines

    Copyright (c) 2000-2014 Chih-Chung Chang and Chih-Jen Lin
    All rights reserved.
    
    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:
    
    1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
    
    2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
    
    3. Neither name of copyright holders nor the names of its contributors
    may be used to endorse or promote products derived from this software
    without specific prior written permission.
    
    
    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
    ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
    LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
    A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR
    CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
    EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
    PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
    LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


 Circular Statistics Toolbox (Directional Statistics)

    Copyright © Philipp Berens, 2006-2012
    All rights reserved.

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are
    met:

        * Redistributions of source code must retain the above copyright
          notice, this list of conditions and the following disclaimer.
        * Redistributions in binary form must reproduce the above copyright
          notice, this list of conditions and the following disclaimer in
          the documentation and/or other materials provided with the distribution
    
   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
   AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
   ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
   LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
   CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
   SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
   INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
   CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
   ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
   POSSIBILITY OF SUCH DAMAGE.

   
 LumenWorks.Framework.IO.CSV.CsvReader - A Fast CSV Reader

   Copyright © Sebastien Lorion, 2005-2011
   All rights reserved.

   MIT license (http://en.wikipedia.org/wiki/MIT_License)

   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights 
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
   of the Software, and to permit persons to whom the Software is furnished to do
   so, subject to the following conditions:

   The above copyright notice and this permission notice shall be included in all 
   copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
   WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
   CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


 Munkres algorithm - The Hungarian method for solving the assignment problem

    The MIT License (MIT)

    Copyright (c) 2000 Robert A. Pilgrim
                       Murray State University
                       Dept. of Computer Science & Information Systems
                       Murray,Kentucky

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.


LIBLINEAR - A library for large linear classification

   Copyright (c) 2007-2011 The LIBLINEAR Project.
   All rights reserved.

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:

      1. Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.

      2. Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

      3. Neither name of copyright holders nor the names of its contributors
      may be used to endorse or promote products derived from this software
      without specific prior written permission.


   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
   ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
   A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR
   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
   
   
Snowball: A language for stemming algorithms   
   
   Copyright (c) 2001, Dr Martin Porter
   Copyright (c) 2004,2005, Richard Boulton
   All rights reserved.
   
   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:
   
     1. Redistributions of source code must retain the above copyright notice,
        this list of conditions and the following disclaimer.
     2. Redistributions in binary form must reproduce the above copyright notice,
        this list of conditions and the following disclaimer in the documentation
        and/or other materials provided with the distribution.
     3. Neither the name of the Snowball project nor the names of its contributors
        may be used to endorse or promote products derived from this software
        without specific prior written permission.
   
   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
   DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
   ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
   ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


 MathExtension: Mathematical extensions and structures for the .NET framework 

    The MIT License(MIT)
    
    Copyright(c) 2015 AJ Richardson
    
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.


 Partitioning around medoids and Voronoi iteration algorithm for 
 the K-Medoids clustering: algorithm implementation and unit tests

    The MIT License (MIT)
    
    Copyright (c) 2017 Ivan Pizhenko
    
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.


  Statistical Quantiles: implementation and unit tests

    The MIT License (MIT)
    
    Copyright (c) 2017 Ivan Pizhenko
    
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.


# MessagePack for C#
=====================================================
MIT License

Copyright (c) 2017 Yoshifumi Kawai

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

lz4net

Copyright (c) 2013-2017, Milosz Krajewski

All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


# zlib.h -- interface of the 'zlib' general purpose compression library
=====================================================
  version 1.2.11, January 15th, 2017

  Copyright (C) 1995-2017 Jean-loup Gailly and Mark Adler

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jean-loup Gailly        Mark Adler
  jloup@gzip.org          madler@alumni.caltech.edu

*/


# Microsoft Public License (Ms-PL)
=====================================================
This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.

A "contributor" is any person that distributes its contribution under this license.

"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.

(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.

(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.

(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.

(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.

(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.


# PDFsharp License
=====================================================
Copyright (c) 2005-2014 empira Software GmbH, Troisdorf (Germany)

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions: 

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software. 

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.


# License - IKVM.net
=====================================================
Copyright (C) 2002, 2003, 2004 Jeroen Frijters

This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.

3. This notice may not be removed or altered from any source distribution.

Jeroen Frijters
jeroen@frijters.net 



# CDK: Chemistry Development Kit
=====================================================
GNU Lesser General Public License, version 2.1 (or later).

The LGPL is compatible with other major open-source licenses. Since Java libraries are dynamically linked there is no restriction in using CDK in proprietary software (see the FSF's LGPL and Java). 
Keep in mind that libraries that parts of the CDK depend on have their own licenses.


# NCDK: The Chemistry Development Kit ported to C&#35;
=====================================================
Copyright (c) 2016-2019 Kazuya Ujihara
NCDK is .NET port of [the Chemistry Development Project (CDK)](https://github.com/cdk/cdk). Functionality is provided for many areas in cheminformatics.

NCDK is covered under LGPL v2.1. The modules are free and open-source and is easily integrated with other open-source or in-house projects.

The current release is based on [CDK 2019-11-27](https://github.com/cdk/cdk/tree/c76443e0a69a4110c6a0fe65704abccce8a435ff) snapshot.

# Agilent MHDAC and MIDAC
=====================================================
AGILENT TECHNOLOGIES, INC. SOFTWARE LICENSE TERMS
FOR THE MASSHUNTER DATA ACCESS COMPONENT RUNTIME VERSION
ATTENTION: USE OF THE SOFTWARE IS SUBJECT TO THE LICENSE TERMS SET FORTH
BELOW.
IF YOU DO NOT AGREE TO THESE LICENSE TERMS, THEN (A) DO NOT INSTALL OR USE THE
SOFTWARE, AND (B) YOU MAY RETURN THE SOFTWARE FOR A FULL REFUND, OR, IF THE
SOFTWARE IS SUPPLIED AS PART OF ANOTHER PRODUCT, YOU MAY RETURN THE ENTIRE
PRODUCT FOR A FULL REFUND. NOTWITHSTANDING ANYTHING TO THE CONTRARY IN THIS
NOTICE, INSTALLING OR OTHERWISE USING THE SOFTWARE INDICATES YOUR ACCEPTANCE OF
THESE TERMS.
AGILENT SOFTWARE LICENSE TERMS
Software. “Software” means the MassHunter Data Access Component Runtime computer program in object code
format.
License Grant. Agilent grants you a non-exclusive, non-transferable license to (a) use one copy of the Software for
internal purposes in accordance with these License Terms and the documentation provided with the Software or (b)
to distribute the Software for non-commercial purposes only and only bundled as part of, and for the sole purpose of
running products supplied by you. This Software is licensed for internal concurrent or network use of an unlimited
number of copies, provided access to this Software and any copies is restricted to your employees, contractors and
other personnel that are working in a capacity that is under your control and on your behalf. If you distribute for
non-commercial purposes only as permitted by this license, you must ensure that a copy of this license is distributed
with the Software and that the recipient of the Software agrees to the terms of this license as a condition of execution
of this Software.
License Restrictions. You may make copies or adaptations of the Software only for archival purposes or only when
copying or adaptation is an essential step in the authorized use of the Software. You must reproduce all copyright
notices in the original Software on all permitted copies or adaptations. You may not offer or provide unrestricted
access to this Software on any public or distributed network.
Upgrades. This license does not entitle you to receive upgrades, updates or technical support. Such services may
be purchased separately.
Ownership. The Software and all copies thereof are owned and copyrighted by Agilent. Agilent retains all right,
title and interest in the Software. Agilent and its third party suppliers may protect their rights in the Software in the
event of any violation of these License Terms.
No Disassembly. You may not disassemble, decompile or otherwise modify the Software without written
authorization from Agilent, except as permitted by law. Upon request, you will provide Agilent with reasonably
detailed information regarding any permitted disassembly, decompilation or modification.
High Risk Activities. The Software is not specifically designed, manufactured or intended for use in the planning,
construction, maintenance or direct operation of a nuclear facility, nor for use in on line control or fail safe operation
of aircraft navigation, control or communication systems, weapon systems or direct life support systems.
Termination. Agilent may terminate your license upon notice for breach of these License Terms. Upon
termination, you must immediately destroy all copies of the Software.
Page 2 of 2
No Warranty. THIS SOFTWARE IS LICENSED "AS IS" AND WITHOUT WARRANTY OF ANY KIND,
EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO THE IMPLIED WARRANTIES
OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A PARTICULAR PURPOSE.
Limitation of Liability. TO THE EXTENT NOT PROHIBITED BY LAW, IN NO EVENT WILL AGILENT BE
LIABLE FOR ANY LOST REVENUE, PROFIT OR DATA, OR FOR SPECIAL, INDIRECT,
CONSEQUENTIAL, INCIDENTAL OR PUNITIVE DAMAGES, HOWEVER CAUSED REGARDLESS OF THE
THEORY OF LIABILITY, ARISING OUT OF OR RELATED TO THE USE OF OR INABILITY TO USE
SOFTWARE, EVEN IF AGILENT HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES. In no
event will Agilent's liability to you, whether in contract, tort (including negligence), or otherwise, exceed the amount
paid by you for the Software. The foregoing limitations will apply even if the above stated warranty fails of its
essential purpose.
Export Requirements. If you export, re-export or import Software, technology or technical data licensed
hereunder, you assume responsibility for complying with applicable laws and regulations and for obtaining required
export and import authorizations. Agilent may terminate this license immediately if you are in violation of any
applicable laws or regulations.
U.S. Government Restricted Rights. Software and technical data rights granted to the federal government include
only those rights customarily provided to end user customers. Agilent provides this customary commercial license
in Software and technical data pursuant to FAR 12.211 (Technical Data) and 12.212 (Computer Software) and, for
the Department of Defense, DFARS 252.227-7015 (Technical Data – Commercial Items) and DFARS 227.7202-3
(Rights in Commercial Computer Software or Computer Software Documentation).

# Bruker tdf-sdk
=====================================================
This is tdf-sdk (version 2.3.x), shared libraries and examples for accessing mass-spectrometry data 
in tdf (tims data) format stored by Bruker timsTOF instruments.

This software uses Intel(R) Math Kernel Library
(http://www.intel.com/software/products/mkl) and other third-party software. A full list of their licenses can be found below.

Intel® Math Kernel Library, http://www.intel.com/software/products/mkl

End User License Agreement for the Intel(R) Software Development Products
(Version May 2012)

1.	LICENSE DEFINITIONS: 

A.	"Materials" are defined as the software, documentation, license key 
codes (if applicable) and other materials, including any updates and upgrade 
thereto, that are provided to you under this Agreement.  Materials also 
include the Redistributables, Cluster OpenMP Library, and Sample Source 
as defined below.

B.	"Redistributables" are the files listed in the following text files that
may be included in the Materials for the applicable Intel Software Development 
Product: clredist.txt, credist.txt, fredist.txt, redist.txt, redist-rt.txt.

C.	"Cluster OpenMP Library", is comprised of the files listed in the 
"clredist.txt" file specified above, is the Intel(R) Cluster OpenMP* Library 
add-on option to the Intel(R) C++ Compiler for Linux* and 
Intel(R) Fortran Compiler for Linux* products ("Intel Compiler for Linux").  
The use of the Cluster OpenMP Library is conditioned on having a valid license 
from Intel for the Cluster OpenMP Library and for either Intel(R) C++ Compiler 
for Linux or Intel(R) Fortran Compiler for Linux, and further is governed by the
terms and conditions of the license agreement for applicable the Intel Compiler 
for Linux.

D.	"Source Code" is defined as the Materials provided in human readable 
format, and includes modification that you make or are made on your behalf.  

E.	"Sample Source" is the Source Code file(s) that: (i) demonstrate certain
limited functions included in the binary libraries of the Intel(R) Integrated 
Performance Primitives ("Intel(R) IPPs"); (ii) are identified as Intel IPP 
sample source code; (iii) are obtained separately from Intel after you register 
your copy of the Intel(R) IPPs product with Intel; and (iv) are subject to all 
of the terms and conditions of this Agreement.

F.	"Microsoft Platforms" means any current and future Microsoft operating 
system products, Microsoft run-time technologies (such as the .NET Framework), 
and Microsoft application platforms 
(such as Microsoft Office or Microsoft Dynamics) that Microsoft offers.

2.	LICENSE GRANT:

A.	Subject to all of the terms and conditions of this Agreement, 
Intel Corporation ("Intel") grants to you a non-exclusive, non-assignable, 
copyright license to use the Materials. 

B.	Subject to all of the terms and conditions of this Agreement, Intel 
grants to you a non-exclusive, non-assignable copyright license to modify the 
Materials, or any portions thereof, that are (i) provided in Source Code form 
or, (ii) are defined as Redistributables and are provided in text form.

C.	Subject to all of the terms and conditions of this Agreement and any 
specific restrictions which may appear in the Redistributables text files, Intel
grants to you a non-exclusive, non-assignable, fully-paid copyright license to 
distribute (except if you received the Materials under an Evaluation License 
as specified below) the Redistributables, including any modifications pursuant 
to Section 2.B, or any portions thereof, as part of the product or application 
you developed using the Materials.  If such application is a software 
development library, then attribution, as specified in the product release notes
of the corresponding Materials shall be displayed prominently in that product's
or application's associated documentation and on the product or application's 
web site (if any). 

3.	LICENSE RESTRICTIONS:

A.	If you receive your first copy of the Materials electronically, and a 
second copy on media, then you may use the second copy only in accordance with 
your applicable license stated in this Agreement, or for backup or archival 
purposes.  You may not provide the second copy to another user.

B.	You may NOT:  (i) use, copy, distribute, or publicly display the 
Materials except as provided in this Agreement; (ii) rent or lease the Materials
to any third party; (iii) assign this Agreement or transfer the Materials 
without the express written consent of Intel; (iv) modify, adapt, or translate 
the Materials in whole or in part except as provided in this Agreement; 
(v) reverse engineer, decompile, or disassemble the Materials; (vi) attempt to 
modify or tamper with the normal function of a license manager that regulates 
usage of the Materials; (vii) distribute, sublicense or transfer the Source 
Code form of any components of the Materials or derivatives thereof to any third
party except as provided in this Agreement; (viii) distribute Redistributables 
except as part of a larger program that adds significant primary functionality 
different from that of the Redistributables; (ix) distribute the 
Redistributables to run on a platform other than a Microsoft Platform if per the
accompanying user documentation the Materials are meant to execute only on a 
Microsoft Platform; (x) include the Redistributables in malicious, deceptive, 
or unlawful programs; or (xi) modify or distribute the Source Code of any 
Redistributable so that any part of it becomes subject to an Excluded License.  
An "Excluded License" is one that requires, as a condition of use, modification,
or distribution, that the licensed software or other software incorporated into,
derived from or distributed with such software (a) be disclosed or distributed 
in Source Code form; (b) be licensed by the user to third parties for the 
purpose of making and/or distributing derivative works; 
or (c) be redistributable at no charge.  Open source software includes, without 
limitation, software licensed or distributed under any of the 
following licenses or distribution models, or licenses or distribution models 
substantially similar to any of the following: (a) GNU's General Public License
(GPL) or Lesser/Library GPL (LGPL), (b) the Artistic License (e.g., PERL), 
(c) the Mozilla Public License, (d) the Netscape Public License, (e) the Sun 
Community Source License (SCSL), (f) the Sun Industry Source License (SISL), 
and (g) the Common Public License (CPL).

C.	The scope and term of your license depends on the type of license you 
are provided by Intel.  The variety of license types are set forth below, which 
may not be available for all "Intel(R) Software Development Products" and 
therefore may not apply to the Materials.  For more information on the types of
licenses, please contact Intel or your sales representative.

i.	PRE-RELEASE LICENSE:  If you are using the Materials under the control
of a pre-release license, (a) the Materials are deemed to be pre-release code 
(e.g., alpha or beta release, etc), which may not be fully functional and which 
Intel may substantially modify in development of a  commercial version, and for 
which Intel makes no assurances that it will ever develop or make generally 
available a commercial version, and (b) if you are an individual, you have the 
right to use the Materials only for the duration of the pre-release term, which 
is specified in the Materials, or until the commercial release, if any, of the 
Materials, whichever is shorter.  You may install copies of the Materials on an 
unlimited number of computers provided that you are the only individual using 
the Materials and only one copy of the Materials is in use at any one time.  
A separate license is required for each additional use and/or individual user 
in all other cases, including without limitation, use by persons, computer 
systems, and other use methods known now and in the future.  If you are an 
entity, Intel grants you the right to designate one individual within your 
organization to have the sole right to use the Materials in the manner 
specified provided above.

ii.	EVALUATION LICENSE: If you are using the Materials under the control 
of an evaluation license, you as an individual may use the Materials only for 
internal evaluation purposes and only for the term of the evaluation, which 
may be controlled by the license key code for the Materials.  
NOTWITHSTANDING ANYTHING TO THE CONTRARY ELSEWHERE IN THIS 
AGREEMENT, YOU MAY NOT DISTRIBUTE ANY PORTION OF THE MATERIALS, AND THE 
APPLICATION AND/OR PRODUCT DEVELOPED BY YOU MAY ONLY BE USED FOR 
EVALUATION PURPOSES AND ONLY FOR THE TERM OF THE EVALUATION.  You may 
install copies of the Materials on a reasonable number of computers to conduct
your evaluation provided that you are the only individual using the Materials 
and only one copy of the Materials is in use at any one time.  A separate 
license is required for each additional use and/or individual user in all other
cases, including without limitation, use by persons, computer systems, and other
use methods known now and in the future.  Intel may provide you with a license 
code key that enables the Materials for an evaluation license.  If you are an 
entity, Intel grants you the right to designate one individual within your 
organization to have the sole right to use the Materials in the manner 
provided above.

iii.	NONCOMMERCIAL-USE LICENSE:  If you are using the Materials under the 
control of a noncommercial-use license, if you are an individual, you as an 
individual may use the Materials only for non-commercial use where you receive 
no fee, salary or any other form of compensation.  The Materials may not be used
for any other purpose, whether "for profit" or "not for profit."  Any work 
performed or produced as a result of use of the Materials cannot be performed 
or produced for the benefit of other parties for a fee, compensation or any 
other 
reimbursement or remuneration.  You may install copies of the Materials on an 
unlimited number of computers provided that you are the only individual using 
the Materials and only one copy of the Materials is in use at any one time.  
A separate license is required for each additional use and/or individual user 
in all other cases, including without limitation, use by persons, computer 
systems, and other methods of use known now and in the future.  Intel will 
provide you with a license code key that enables the Materials for a 
noncommercial-use license.  If you obtained a time-limited noncommercial-use 
license, the duration (time period) of your license and your ability to use the 
Materials is limited to the time period of the obtained license, which is 
controlled by the license key code for the Materials.  If you are an entity, 
Intel grants you the right to designate one individual within your organization 
to have the sole right to use the Materials in the manner provided above.

iv.	SINGLE-USER LICENSE: If you are using the Materials under the control 
of a single-user license, you as an individual may install and use the Materials
on an unlimited number of computers provided that you are the only individual 
using the Materials and only one copy of the Materials is in use at any one 
time.  A separate license is required for each additional use and/or individual 
user in all other cases, including without limitation, use by persons, computer 
systems, and other methods of use known now and in the future.  Intel will 
provide you with a license code key that enables the Materials for a single-user
license.  If you obtained a time-limited single-user license, the term of your 
license and your ability to use the Materials is limited to the specified time 
period, which is controlled by the license key code for the Materials.  If you 
are an entity, Intel grants you the right to designate one individual within 
your organization to have the sole right to use the Materials in the manner 
provided above.

v.	NODE-LOCKED LICENSE: If you are using the Materials under the control of
 a node-locked license, you may use the Materials only on a single designated 
computer by no more than the authorized number of concurrent users.  A separate 
license is required for each additional concurrent user and use, and/or computer
systems in all other cases, including without limitation, use by persons, 
computer systems, and other methods of use known now and in the future.  Intel 
will provide you with a license code key that enables the Materials for a Node-
Locked license up to the authorized number of concurrent users.  If you obtained
a time-limited node-locked license, the term of your license and your ability to
use the Materials is limited to the specified time, which is controlled by the 
license key code for the Materials.

vi.	FLOATING LICENSE: If you are using the Materials under the control of a 
floating license, you may (a) install the Materials on an unlimited number of 
computers that are connected to the designated network and (b) use the Material 
by no more than the authorized number of concurrent individual users.  
A separate license is required for each additional concurrent individual user 
and each additional use by a computer system and/or network on which the 
Materials are used. You understand that you must obtain a  separate license for 
every and any use of the Materials under a floating license, regardless of 
whether such use is, without limitation, by persons, computer systems, and other
methods of use known now and in the future.  Intel will provide you with a 
license code key that enables the Materials for a floating license up to the 
authorized number of concurrent users.  If you obtained a time-limited Floating 
license, the duration (time period) of your license and your ability to use the 
Materials is limited to the time period of the obtained license, which is 
controlled by the license key code for the Materials.  Intel Library Floating 
License: If the Materials are the Intel(R) Math Kernel Library or the Intel(R) 
Integrated Performance Primitives Library or the Intel(R) Threading Building 
Blocks (either "Intel Library"), then the Intel Library is provided to you as an
add-on option to either the Intel(R) C++ Compiler product or the Intel(R) 
Fortran Compiler product (either, an "Intel Compiler") for which you have a 
Floating license, and as such, in addition to the terms and conditions above, 
the Intel Library may only be used by the authorized concurrent users (as noted 
above) of that Intel Compiler Floating license.

D.	DISTRIBUTION:  Distribution of the Redistributables is also subject to 
the following limitations:  You (i) shall be solely responsible to your 
customers for any update or support obligation or other liability which may 
arise from the distribution, (ii) shall not make any statement that your product
is "certified", or that its performance is guaranteed, by Intel, (iii) shall not
use Intel's name or trademarks to market your product without written 
permission, (iv) shall use a license agreement that prohibits disassembly and 
reverse engineering of the Redistributables, (v) shall indemnify, hold harmless,
and defend Intel and its suppliers from and against any claims or lawsuits, 
including attorney's fees, that arise or result from your distribution of 
any product.

E.	Intel(R) Integrated Performance Primitives (Intel IPP). The following 
terms and conditions apply only to the Intel IPP.

i.	Notwithstanding anything in this Agreement to the contrary, if you 
implement the Sample Sources in your application or if you use Intel IPP to 
implement algorithms that are protected by others' licenses then you may need 
additional licenses from various entities. Should any such additional licenses 
be required, you are solely responsible for obtaining any such licenses and 
agree to obtain any such licenses at your own expense.

ii.	Notwithstanding anything herein to the contrary, a valid license to 
Intel IPP is a prerequisite to any license for Sample Source, and possession of 
Sample Source does not grant any license to Intel IPP (or any portion thereof).
To access Sample Source, you must first register your licensed copy of the Intel
IPP with Intel.  By downloading, installing or copying any Sample Source file, 
you agree to be bound by terms of this Agreement.

F.	SOFTWARE TRANSFER:  Except for Pre-Release Licenses or Evaluation 
Licenses or Non-Commercial Licenses, as specified above, you may permanently 
transfer the Materials you received pursuant to a license type listed in 
Section 3(C) above, and all of your rights under this Agreement, to another 
party ("Recipient") solely in conjunction with a change of ownership, merger, 
acquisition, sale or transfer of all, substantially all or any part of your 
business or assets or otherwise, either voluntarily, by operation of law of 
otherwise subject to the following: You must notify Intel of the transfer by 
sending a letter to Intel (i) identifying the legal entities of Recipient and 
you, (ii) identifying the Materials (i.e., the specific Intel software products)
and the associated serial numbers to be transferred, (iii) certifying that you 
retain no copies of the Materials, (iv) certifying that the Recipient has agreed
in writing to be bound by all of the terms and conditions of this Agreement, (v) 
certifying that the Recipient has been notified that in order to receive support
from Intel for the Materials they must notify Intel in writing of the transfer 
and provide Intel with the information specified in subsection (ii) above along 
with the name and email address of the individual assigned to use the Materials, 
and (vi) providing your email address so that we may confirm receipt of your 
letter.  Please send such letter to:

Intel Corporation
2111 NE 25th Avenue
Hillsboro, OR 97124
Attn: DPD Contracts Management, JF1-15

4.	COPYRIGHT: Title to the Materials, modifications thereto provided by 
Intel and all copies thereof remain with Intel or its suppliers.  The Materials 
are protected by intellectual property rights, including without limitation, 
United States copyright laws and international treaty provisions.  You will not 
remove any copyright or other proprietary notice from the Materials.  You agree 
to prevent any unauthorized copying of the Materials.  Except as expressly 
provided herein, no license or right is granted to you directly or by 
implication, inducement, estoppel or otherwise; specifically Intel does not 
grant any express or implied right to you under Intel patents, copyrights, 
trademarks, or trade secrets.

5.	NO WARRANTY, NO SUPPORT AND LIMITED REPLACEMENT:  THE MATERIALS AND 
INFORMATION ARE PROVIDED "AS IS" WITH NO WARRANTIES, EXPRESS OR IMPLIED. INTEL 
SPECIFICALLY DISCLAIMS ANY AND ALL WARRANTIES, INCLUDING WITHOUT LIMITATION,  
ANY IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, 
NON-INFRINGEMENT OF INTELLECTUAL PROPERTY RIGHTS, OR ANY WARRANTY OTHERWISE 
ARISING OUT OF ANY PROPOSAL, SPECIFICATION, OR SAMPLE.  If the media on which 
the Materials are furnished are found to be defective in material or workmanship
under normal use for a period of ninety (90) days from the date of receipt, 
Intel's entire liability and your exclusive remedy shall be the replacement of 
the media.  This offer is void if the media defect results from accident, abuse,
or misapplication.

Intel may make changes to the Materials, or to items referenced therein, at any 
time without notice, but is not obligated to support, update or provide training
for the Materials. Intel may in its sole discretion offer such support, update 
or training services under separate terms at Intel's then-current rates. You may
request additional information on Intel's service offerings from an Intel sales 
representative.

6.	LIMITATION OF LIABILITY:  NEITHER INTEL NOR ITS SUPPLIERS SHALL BE 
LIABLE FOR ANY DAMAGES WHATSOEVER (INCLUDING, WITHOUT LIMITATION, DAMAGES FOR 
LOSS OF BUSINESS PROFITS, BUSINESS INTERRUPTION, LOSS OF BUSINESS INFORMATION, 
OR OTHER LOSS) ARISING OUT OF THE USE OF OR INABILITY TO USE THE SOFTWARE, EVEN
IF INTEL HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.  BECAUSE SOME 
JURISDICTIONS PROHIBIT THE EXCLUSION OR LIMITATION OF LIABILITY FOR 
CONSEQUENTIAL OR INCIDENTAL DAMAGES, THE ABOVE LIMITATION MAY NOT APPLY TO 
YOU.

7.	UNAUTHORIZED USE:  THE MATERIALS ARE NOT DESIGNED, INTENDED, OR 
AUTHORIZED FOR USE IN ANY TYPE OF SYSTEM OR APPLICATION IN WHICH THE FAILURE OF
THE MATERIALS COULD CREATE A SITUATION WHERE PERSONAL INJURY OR DEATH MAY OCCUR 
(e.g.,  MEDICAL SYSTEMS, LIFE SUSTAINING OR LIFE SAVING SYSTEMS).  Should you 
use the Materials for any such unintended or unauthorized use, you hereby 
indemnify, defend, and hold Intel and its officers, subsidiaries and affiliates 
harmless against all claims, costs, damages, and expenses, and reasonable 
attorney fees arising out of, directly or indirectly, such use and any claim of 
product liability, personal injury or death associated with such unintended or 
unauthorized use, even if such claim alleges that Intel was negligent regarding 
the design or manufacture of the Materials.

8.	USER SUBMISSIONS:  This Agreement does not obligate you to provide Intel
with materials, information, comments, suggestions or other communication 
regarding the Materials.  However, you agree that any material, information, 
comments, suggestions or other communication you transmit or post to an Intel 
website (including but not limited to, submissions to the Intel Premier Support 
and/or other customer support websites or online portals) or provide to Intel 
under this Agreement related to the features, functions, performance or use 
of the Materials are deemed non-confidential and non-proprietary 
("Communications").  Intel will have no obligations with respect to the 
Communications.  You hereby grant to Intel a non-exclusive, perpetual, 
irrevocable, royalty-free, copyright license to copy, modify, create derivative 
works, publicly display, disclose, distribute, license and sublicense through 
multiple tiers of distribution and licensees, incorporate and otherwise use the 
Communications and all data, images, sounds, text, and other things embodied 
therein, including derivative works thereto, for any and all commercial or 
non-commercial purposes. You are prohibited from posting or transmitting to or 
from an Intel website or provide to Intel any unlawful, threatening, libelous, 
defamatory, obscene, pornographic, or other material that would violate any law.
If you wish to provide Intel with information that you intend to be treated as 
confidential information, Intel requires that such confidential information be 
provided pursuant to a non-disclosure agreement ("NDA"), so please contact your 
Intel representative to ensure the proper NDA is in place.

Nothing in this Agreement will be construed as preventing Intel from reviewing 
your Communications and errors or defects in Intel products discovered while 
reviewing your Communications. Furthermore, nothing in this Agreement will be 
construed as preventing Intel from implementing independently-developed 
enhancements to Intel's own error diagnosis methodology to detect errors or 
defects in Intel products discovered while reviewing your Communications or to 
implement bug fixes or enhancements in Intel products. The foregoing may include
the right to include your Communications in regression test suites. 

9.	CONSENT.  You agree that Intel, its subsidiaries or suppliers may 
collect and use technical and related information, including but not limited to 
technical information about your computer, system and application software, and 
peripherals, that is gathered periodically to facilitate the provision of 
software updates, product support and other services to you (if any) related to 
the Materials, and to verify compliance with the terms of this Agreement.  Intel
may use this information, as long as it is in a form that does not personally 
identify you, to improve our products or to develop and provide services or 
technologies to you.

10.	TERMINATION OF THIS LICENSE: This Agreement becomes effective on the 
date you accept this Agreement and will continue until terminated as provided 
for in this Agreement.  If you are using the Materials under the control of a 
time-limited license, for example an Evaluation License, this Agreement 
terminates without notice on the last day of the time period, which is specified
in the Materials, and/or controlled by the license key code for the Materials. 
Intel may terminate this license immediately if you are in breach of any of its 
terms and conditions and such breach is not cured within thirty (30) days of 
written notice from Intel.  Upon termination, you will immediately return to 
Intel or destroy the Materials and all copies thereof.  In the event of 
termination of this Agreement, the license grant to any Redistributables 
distributed by you in accordance with the terms and conditions of this 
Agreement, prior to the effective date of such termination, shall survive any 
such termination of this Agreement

11.	U.S. GOVERNMENT RESTRICTED RIGHTS: The technical data and computer 
software covered by this license is a "Commercial Item," as such term is defined
by the FAR 2.101 (48 C.F.R. 2.101) and is "commercial computer software" and 
"commercial computer software documentation" as specified under FAR 12.212 
(48 C.F.R. 12.212) or DFARS 227.7202 (48 C.F.R. 227.7202), as applicable. This 
commercial computer software and related documentation is provided to end users 
for use by and on behalf of the U.S. Government, with only those rights as are 
granted to all other end users pursuant to the terms and conditions herein. Use 
for or on behalf of the U.S. Government is permitted only if the party acquiring
or using this software is properly authorized by an appropriate U.S. Government 
official. This use by or for the U.S. Government clause is in lieu of, and 
supersedes, any other FAR, DFARS, or other provision that addresses Government 
rights in the computer software or documentation covered by this license.  All 
copyright licenses granted to the U.S. Government are coextensive with the 
technical data and computer software licenses granted herein. The U.S. 
Government shall only have the right to reproduce, distribute, perform, display,
and prepare derivative works as needed to implement those rights.

12.	GENERAL PROVISIONS

A.	ENTIRE AGREEMENT: This Agreement is intended to be the entire agreement 
between you and Intel with respect to matters contained herein, and supersedes 
all prior or contemporaneous agreements and negotiations with respect to those 
matters.  No waiver of any breach or default shall constitute a waiver of any 
subsequent breach or default.  If any provision of this Agreement is determined 
by a court to be unenforceable, you and Intel will deem the provision to be 
modified to the extent necessary to allow it to be enforced to the extent 
permitted by law, or if it cannot be modified, the provision will be severed and
deleted from this Agreement, and the remainder of the Agreement will continue in
effect.  Any change, modification or waiver to this Agreement must be in writing
and signed by an authorized representative of you and an officer (or delegate) 
of Intel, and must specifically identify this Agreement by its title (e.g., 
"End User License Agreement for the Intel(R) Software Development Products") and
version, i.e., May 2012).

B.	APPLICABLE LAWS: Any claim arising under or relating to this Agreement 
shall be governed by the internal substantive laws of the State of Delaware, 
without regard to principles of conflict of laws.  You agree that the terms of 
the United Nations Convention on Contracts for the Sale of Goods do not apply 
to this Agreement. You agree that your distribution and export/re-export of the 
Software and permitted modifications shall be in compliance with the laws, 
regulations, orders or other restrictions of applicable export laws.

13.	THIRD PARTY PROGRAMS.  The Materials may include third party programs 
or materials that are governed by the third party's license terms, including 
without limitation, open source software.  The license terms associated with 
such third party programs or materials govern your use of same, and Intel is 
not liable for such third party programs or materials.

* Other names and brands may be claimed as the property of others

======================================

zlib, http://www.zlib.org

/* zlib.h -- interface of the 'zlib' general purpose compression library
  version 1.2.8, April 28th, 2013

  Copyright (C) 1995-2013 Jean-loup Gailly and Mark Adler

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jean-loup Gailly        Mark Adler
  jloup@gzip.org          madler@alumni.caltech.edu

*/

======================================

Numeric Library Bindings for Boost UBlas, http://mathema.tician.de/software/boost-numeric-bindings/

Boost Software License - Version 1.0 - August 17th, 2003

Permission is hereby granted, free of charge, to any person or organization
obtaining a copy of the software and accompanying documentation covered by
this license (the "Software") to use, reproduce, display, distribute,
execute, and transmit the Software, and to prepare derivative works of the
Software, and to permit third-parties to whom the Software is furnished to
do so, all subject to the following:

The copyright notices in the Software and this entire statement, including
the above license grant, this restriction and the following disclaimer,
must be included in all copies of the Software, in whole or in part, and
all derivative works of the Software, unless such copies or derivative
works are solely in the form of machine-executable object code generated by
a source language processor.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

======================================

Boost.Process, http://www.highscore.de/boost/process/

Boost Software License - Version 1.0 - August 17th, 2003

Permission is hereby granted, free of charge, to any person or organization
obtaining a copy of the software and accompanying documentation covered by
this license (the "Software") to use, reproduce, display, distribute,
execute, and transmit the Software, and to prepare derivative works of the
Software, and to permit third-parties to whom the Software is furnished to
do so, all subject to the following:

The copyright notices in the Software and this entire statement, including
the above license grant, this restriction and the following disclaimer,
must be included in all copies of the Software, in whole or in part, and
all derivative works of the Software, unless such copies or derivative
works are solely in the form of machine-executable object code generated by
a source language processor.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

======================================

boost, http://www.boost.org/

Boost Software License - Version 1.0 - August 17th, 2003

Permission is hereby granted, free of charge, to any person or organization
obtaining a copy of the software and accompanying documentation covered by
this license (the "Software") to use, reproduce, display, distribute,
execute, and transmit the Software, and to prepare derivative works of the
Software, and to permit third-parties to whom the Software is furnished to
do so, all subject to the following:

The copyright notices in the Software and this entire statement, including
the above license grant, this restriction and the following disclaimer,
must be included in all copies of the Software, in whole or in part, and
all derivative works of the Software, unless such copies or derivative
works are solely in the form of machine-executable object code generated by
a source language processor.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

======================================

UTF8-CPP, http://utfcpp.sourceforge.net/

// Copyright 2006 Nemanja Trifunovic

/*
Permission is hereby granted, free of charge, to any person or organization
obtaining a copy of the software and accompanying documentation covered by
this license (the "Software") to use, reproduce, display, distribute,
execute, and transmit the Software, and to prepare derivative works of the
Software, and to permit third-parties to whom the Software is furnished to
do so, all subject to the following:

The copyright notices in the Software and this entire statement, including
the above license grant, this restriction and the following disclaimer,
must be included in all copies of the Software, in whole or in part, and
all derivative works of the Software, unless such copies or derivative
works are solely in the form of machine-executable object code generated by
a source language processor.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
*/

======================================

SQLite, http://www.sqlite.org/

All of the code and documentation in SQLite has been dedicated to the
public domain by the authors. All code authors, and representatives of
the companies they work for, have signed affidavits dedicating their
contributions to the public domain and originals of those signed
affidavits are stored in a firesafe at the main offices of
Hwaci. Anyone is free to copy, modify, publish, use, compile, sell, or
distribute the original SQLite code, either in source code form or as
a compiled binary, for any purpose, commercial or non-commercial, and
by any means.

The previous paragraph applies to the deliverable code and
documentation in SQLite - those parts of the SQLite library that you
actually bundle and ship with a larger application. Some scripts used
as part of the build process (for example the "configure" scripts
generated by autoconf) might fall under other open-source
licenses. Nothing from these build scripts ever reaches the final
deliverable SQLite library, however, and so the licenses associated
with those scripts should not be a factor in assessing your rights to
copy and use the SQLite library.

All of the deliverable code in SQLite has been written from
scratch. No code has been taken from other projects or from the open
internet. Every line of code can be traced back to its original
author, and all of those authors have public domain dedications on
file. So the SQLite code base is clean and is uncontaminated with
licensed code from other projects.

======================================

CppSQLite V3.2, http://www.codeproject.com/Articles/6343/CppSQLite-C-Wrapper-for-SQLite

Copyright (c) 2004..2007 Rob Groves. All Rights Reserved. rob.groves@btinternet.com

Permission to use, copy, modify, and distribute this software and its
documentation for any purpose, without fee, and without a written
agreement, is hereby granted, provided that the above copyright notice, 
this paragraph and the following two paragraphs appear in all copies, 
modifications, and distributions.

IN NO EVENT SHALL THE AUTHOR BE LIABLE TO ANY PARTY FOR DIRECT,
INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES, INCLUDING LOST
PROFITS, ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS DOCUMENTATION,
EVEN IF THE AUTHOR HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

THE AUTHOR SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
PARTICULAR PURPOSE. THE SOFTWARE AND ACCOMPANYING DOCUMENTATION, IF
ANY, PROVIDED HEREUNDER IS PROVIDED "AS IS". THE AUTHOR HAS NO OBLIGATION
TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.

======================================

The VIGRA Computer Vision Library, http://ukoethe.github.io/vigra/

Permission is hereby granted, free of charge, to any person    
obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without        
restriction, including without limitation the rights to use,   
copy, modify, merge, publish, distribute, sublicense, and/or   
sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following       
conditions:                                                    
                                                               
The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the          
Software.                                                      
                                                               
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND       
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT    
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,   
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING   
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR  
OTHER DEALINGS IN THE SOFTWARE.

======================================

Zstandard, http://facebook.github.io/zstd/

BSD License

For Zstandard software

Copyright (c) 2016-present, Facebook, Inc. All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

 * Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

 * Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

 * Neither the name Facebook nor the names of its contributors may be used to
   endorse or promote products derived from this software without specific
   prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

======================================

ReactiveProperty, (https://github.com/runceel/ReactiveProperty)

The MIT License (MIT)

Copyright (c) 2018 neuecc, xin9le, okazuki

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

======================================

Reactive Extensions (https://github.com/dotnet/reactive)

The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

======================================

XamlBehaviors for WPF (https://github.com/Microsoft/XamlBehaviorsWpf)

The MIT License (MIT)

Copyright (c) 2015 Microsoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

======================================

Microsoft.CodeAnalysis.CSharp, Microsoft.CodeAnalysis.Common (https://github.com/dotnet/roslyn)

The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

======================================

Microsfot.CodeAnalysis.Analyzers (https://github.com/dotnet/roslyn-analyzers)

The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

======================================

System.Resources.Extensions (https://github.com/dotnet/runtime)

The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

======================================

R.NET (https://github.com/rdotnet/rdotnet)

The MIT License (MIT)

Copyright (c) 2014-2020 Jean-Michel Perraud
Copyright (c) 2019-2020 Luke Rasmussen
Copyright (c) 2018 David Pendray
Copyright (c) 2017 Guillaume Jamet
Copyright (c) 2016 Tomas Petricek, Wei Lu
Copyright (c) 2015 Nigel Delaney, Yuanhe Huang
Copyright (c) 2014 skuyguy94, David Charbonneau
Copyright (c) 2013 Kosei ABE, evolvedmicrobe

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
