/* Copyright (C) 2005-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Isomorphisms;
using NCDK.Isomorphisms.Matchers;
using NCDK.SMARTS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// <see cref="IFingerprinter"/> that gives a bit set which has a size equal to the number
    /// of substructures it was constructed from. A set bit indicates that that
    /// substructure was found at least once in the molecule for which the
    /// fingerprint was calculated. The fingerprint currently supports 307
    /// substructures, listed below:
    /// </summary>
    /// <list type="table">
    /// <thead>
    /// <item>
    /// <term>Bit position</term><term>Description</term><term>Pattern</term>
    /// </item>
    /// </thead>
    /// <tbody>
    /// <item><term>0</term><term>Primary carbon</term><term>[CX4H3][#6]</term></item>
    /// <item><term>1</term><term>Secondary carbon</term><term>[CX4H2]([#6])[#6]</term></item>
    /// <item><term>2</term><term>Tertiary carbon</term><term>[CX4H1]([#6])([#6])[#6]</term></item>
    /// <item><term>3</term><term>Quaternary carbon</term><term>[CX4]([#6])([#6])([#6])[#6]</term></item>
    /// <item><term>4</term><term>Alkene</term><term>[CX3;$([H2]),$([H1][#6]),$(C([#6])[#6])]=[CX3;$([H2]),$([H1][#6]),$(C([#6])[#6])]</term></item>
    /// <item><term>5</term><term>Alkyne</term><term>[CX2]#[CX2]</term></item>
    /// <item><term>6</term><term>Allene</term><term>[CX3]=[CX2]=[CX3]</term></item>
    /// <item><term>7</term><term>Alkylchloride</term><term>[ClX1][CX4]</term></item>
    /// <item><term>8</term><term>Alkylfluoride</term><term>[FX1][CX4]</term></item>
    /// <item><term>9</term><term>Alkylbromide</term><term>[BrX1][CX4]</term></item>
    /// <item><term>10</term><term>Alkyliodide</term><term>[IX1][CX4]</term></item>
    /// <item><term>11</term><term>Alcohol</term><term>[OX2H][CX4;!$(C([OX2H])[O,S,#7,#15])]</term></item>
    /// <item><term>12</term><term>Primary alcohol</term><term>[OX2H][CX4H2;!$(C([OX2H])[O,S,#7,#15])]</term></item>
    /// <item><term>13</term><term>Secondary alcohol</term><term>[OX2H][CX4H;!$(C([OX2H])[O,S,#7,#15])]</term></item>
    /// <item><term>14</term><term>Tertiary alcohol</term><term>[OX2H][CX4D4;!$(C([OX2H])[O,S,#7,#15])]</term></item>
    /// <item><term>15</term><term>Dialkylether</term><term>[OX2]([CX4;!$(C([OX2])[O,S,#7,#15,F,Cl,Br,I])])[CX4;!$(C([OX2])[O,S,#7,#15])]</term></item>
    /// <item><term>16</term><term>Dialkylthioether</term><term>[SX2]([CX4;!$(C([OX2])[O,S,#7,#15,F,Cl,Br,I])])[CX4;!$(C([OX2])[O,S,#7,#15])]</term></item>
    /// <item><term>17</term><term>Alkylarylether</term><term>[OX2](c)[CX4;!$(C([OX2])[O,S,#7,#15,F,Cl,Br,I])]</term></item>
    /// <item><term>18</term><term>Diarylether</term><term>[c][OX2][c]</term></item>
    /// <item><term>19</term><term>Alkylarylthioether</term><term>[SX2](c)[CX4;!$(C([OX2])[O,S,#7,#15,F,Cl,Br,I])]</term></item>
    /// <item><term>20</term><term>Diarylthioether</term><term>[c][SX2][c]</term></item>
    /// <item><term>21</term><term>Oxonium</term><term>[O+;!$([O]~[!#6]);!$([S]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>22</term><term>Amine</term><term>[NX3+0,NX4+;!$([N]~[!#6]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>23</term><term>Primary aliph amine</term><term>[NX3H2+0,NX4H3+;!$([N][!C]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>24</term><term>Secondary aliph amine</term><term>[NX3H1+0,NX4H2+;!$([N][!C]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>25</term><term>Tertiary aliph amine</term><term>[NX3H0+0,NX4H1+;!$([N][!C]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>26</term><term>Quaternary aliph ammonium</term><term>[NX4H0+;!$([N][!C]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>27</term><term>Primary arom amine</term><term>[NX3H2+0,NX4H3+]c</term></item>
    /// <item><term>28</term><term>Secondary arom amine</term><term>[NX3H1+0,NX4H2+;!$([N][!c]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>29</term><term>Tertiary arom amine</term><term>[NX3H0+0,NX4H1+;!$([N][!c]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>30</term><term>Quaternary arom ammonium</term><term>[NX4H0+;!$([N][!c]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>31</term><term>Secondary mixed amine</term><term>[NX3H1+0,NX4H2+;$([N]([c])[C]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>32</term><term>Tertiary mixed amine</term><term>[NX3H0+0,NX4H1+;$([N]([c])([C])[#6]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>33</term><term>Quaternary mixed ammonium</term><term>[NX4H0+;$([N]([c])([C])[#6][#6]);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>34</term><term>Ammonium</term><term>[N+;!$([N]~[!#6]);!$(N=*);!$([N]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>35</term><term>Alkylthiol</term><term>[SX2H][CX4;!$(C([SX2H])~[O,S,#7,#15])]</term></item>
    /// <item><term>36</term><term>Dialkylthioether</term><term>[SX2]([CX4;!$(C([SX2])[O,S,#7,#15,F,Cl,Br,I])])[CX4;!$(C([SX2])[O,S,#7,#15])]</term></item>
    /// <item><term>37</term><term>Alkylarylthioether</term><term>[SX2](c)[CX4;!$(C([SX2])[O,S,#7,#15])]</term></item>
    /// <item><term>38</term><term>Disulfide</term><term>[SX2D2][SX2D2]</term></item>
    /// <item><term>39</term><term>1,2-Aminoalcohol</term><term>[OX2H][CX4;!$(C([OX2H])[O,S,#7,#15,F,Cl,Br,I])][CX4;!$(C([N])[O,S,#7,#15])][NX3;!$(NC=[O,S,N])]</term></item>
    /// <item><term>40</term><term>1,2-Diol</term><term>[OX2H][CX4;!$(C([OX2H])[O,S,#7,#15])][CX4;!$(C([OX2H])[O,S,#7,#15])][OX2H]</term></item>
    /// <item><term>41</term><term>1,1-Diol</term><term>[OX2H][CX4;!$(C([OX2H])([OX2H])[O,S,#7,#15])][OX2H]</term></item>
    /// <item><term>42</term><term>Hydroperoxide</term><term>[OX2H][OX2]</term></item>
    /// <item><term>43</term><term>Peroxo</term><term>[OX2D2][OX2D2]</term></item>
    /// <item><term>44</term><term>Organolithium compounds</term><term>[LiX1][#6,#14]</term></item>
    /// <item><term>45</term><term>Organomagnesium compounds</term><term>[MgX2][#6,#14]</term></item>
    /// <item><term>46</term><term>Organometallic compounds</term><term>[!#1;!#5;!#6;!#7;!#8;!#9;!#14;!#15;!#16;!#17;!#33;!#34;!#35;!#52;!#53;!#85]~[#6;!-]</term></item>
    /// <item><term>47</term><term>Aldehyde</term><term>[$([CX3H][#6]),$([CX3H2])]=[OX1]</term></item>
    /// <item><term>48</term><term>Ketone</term><term>[#6][CX3](=[OX1])[#6]</term></item>
    /// <item><term>49</term><term>Thioaldehyde</term><term>[$([CX3H][#6]),$([CX3H2])]=[SX1]</term></item>
    /// <item><term>50</term><term>Thioketone</term><term>[#6][CX3](=[SX1])[#6]</term></item>
    /// <item><term>51</term><term>Imine</term><term>[NX2;$([N][#6]),$([NH]);!$([N][CX3]=[#7,#8,#15,#16])]=[CX3;$([CH2]),$([CH][#6]),$([C]([#6])[#6])]</term></item>
    /// <item><term>52</term><term>Immonium</term><term>[NX3+;!$([N][!#6]);!$([N][CX3]=[#7,#8,#15,#16])]</term></item>
    /// <item><term>53</term><term>Oxime</term><term>[NX2](=[CX3;$([CH2]),$([CH][#6]),$([C]([#6])[#6])])[OX2H]</term></item>
    /// <item><term>54</term><term>Oximether</term><term>[NX2](=[CX3;$([CH2]),$([CH][#6]),$([C]([#6])[#6])])[OX2][#6;!$(C=[#7,#8])]</term></item>
    /// <item><term>55</term><term>Acetal</term><term>[OX2]([#6;!$(C=[O,S,N])])[CX4;!$(C(O)(O)[!#6])][OX2][#6;!$(C=[O,S,N])]</term></item>
    /// <item><term>56</term><term>Hemiacetal</term><term>[OX2H][CX4;!$(C(O)(O)[!#6])][OX2][#6;!$(C=[O,S,N])]</term></item>
    /// <item><term>57</term><term>Aminal</term><term>[NX3v3;!$(NC=[#7,#8,#15,#16])]([#6])[CX4;!$(C(N)(N)[!#6])][NX3v3;!$(NC=[#7,#8,#15,#16])][#6]</term></item>
    /// <item><term>58</term><term>Hemiaminal</term><term>[NX3v3;!$(NC=[#7,#8,#15,#16])]([#6])[CX4;!$(C(N)(N)[!#6])][OX2H]</term></item>
    /// <item><term>59</term><term>Thioacetal</term><term>[SX2]([#6;!$(C=[O,S,N])])[CX4;!$(C(S)(S)[!#6])][SX2][#6;!$(C=[O,S,N])]</term></item>
    /// <item><term>60</term><term>Thiohemiacetal</term><term>[SX2]([#6;!$(C=[O,S,N])])[CX4;!$(C(S)(S)[!#6])][OX2H]</term></item>
    /// <item><term>61</term><term>Halogen acetal like</term><term>[NX3v3,SX2,OX2;!$(*C=[#7,#8,#15,#16])][CX4;!$(C([N,S,O])([N,S,O])[!#6])][FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>62</term><term>Acetal like</term><term>[NX3v3,SX2,OX2;!$(*C=[#7,#8,#15,#16])][CX4;!$(C([N,S,O])([N,S,O])[!#6])][FX1,ClX1,BrX1,IX1,NX3v3,SX2,OX2;!$(*C=[#7,#8,#15,#16])]</term></item>
    /// <item><term>63</term><term>Halogenmethylen ester and similar</term><term>[NX3v3,SX2,OX2;$(**=[#7,#8,#15,#16])][CX4;!$(C([N,S,O])([N,S,O])[!#6])][FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>64</term><term>NOS methylen ester and similar</term><term>[NX3v3,SX2,OX2;$(**=[#7,#8,#15,#16])][CX4;!$(C([N,S,O])([N,S,O])[!#6])][NX3v3,SX2,OX2;!$(*C=[#7,#8,#15,#16])]</term></item>
    /// <item><term>65</term><term>Hetero methylen ester and similar</term><term>[NX3v3,SX2,OX2;$(**=[#7,#8,#15,#16])][CX4;!$(C([N,S,O])([N,S,O])[!#6])][FX1,ClX1,BrX1,IX1,NX3v3,SX2,OX2;!$(*C=[#7,#8,#15,#16])]</term></item>
    /// <item><term>66</term><term>Cyanhydrine</term><term>[NX1]#[CX2][CX4;$([CH2]),$([CH]([CX2])[#6]),$(C([CX2])([#6])[#6])][OX2H]</term></item>
    /// <item><term>67</term><term>Chloroalkene</term><term>[ClX1][CX3]=[CX3]</term></item>
    /// <item><term>68</term><term>Fluoroalkene</term><term>[FX1][CX3]=[CX3]</term></item>
    /// <item><term>69</term><term>Bromoalkene</term><term>[BrX1][CX3]=[CX3]</term></item>
    /// <item><term>70</term><term>Iodoalkene</term><term>[IX1][CX3]=[CX3]</term></item>
    /// <item><term>71</term><term>Enol</term><term>[OX2H][CX3;$([H1]),$(C[#6])]=[CX3]</term></item>
    /// <item><term>72</term><term>Endiol</term><term>[OX2H][CX3;$([H1]),$(C[#6])]=[CX3;$([H1]),$(C[#6])][OX2H]</term></item>
    /// <item><term>73</term><term>Enolether</term><term>[OX2]([#6;!$(C=[N,O,S])])[CX3;$([H0][#6]),$([H1])]=[CX3]</term></item>
    /// <item><term>74</term><term>Enolester</term><term>[OX2]([CX3]=[OX1])[#6X3;$([#6][#6]),$([H1])]=[#6X3;!$(C[OX2H])]</term></item>
    /// <item><term>75</term><term>Enamine</term><term>[NX3;$([NH2][CX3]),$([NH1]([CX3])[#6]),$([N]([CX3])([#6])[#6]);!$([N]*=[#7,#8,#15,#16])][CX3;$([CH]),$([C][#6])]=[CX3]</term></item>
    /// <item><term>76</term><term>Thioenol</term><term>[SX2H][CX3;$([H1]),$(C[#6])]=[CX3]</term></item>
    /// <item><term>77</term><term>Thioenolether</term><term>[SX2]([#6;!$(C=[N,O,S])])[CX3;$(C[#6]),$([CH])]=[CX3]</term></item>
    /// <item><term>78</term><term>Acylchloride</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[ClX1]</term></item>
    /// <item><term>79</term><term>Acylfluoride</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[FX1]</term></item>
    /// <item><term>80</term><term>Acylbromide</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[BrX1]</term></item>
    /// <item><term>81</term><term>Acyliodide</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[IX1]</term></item>
    /// <item><term>82</term><term>Acylhalide</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>83</term><term>Carboxylic acid</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>84</term><term>Carboxylic ester</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>85</term><term>Lactone</term><term>[#6][#6X3R](=[OX1])[#8X2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>86</term><term>Carboxylic anhydride</term><term>[CX3;$([H0][#6]),$([H1])](=[OX1])[#8X2][CX3;$([H0][#6]),$([H1])](=[OX1])</term></item>
    /// <item><term>87</term><term>Carboxylic acid derivative</term><term>[$([#6X3H0][#6]),$([#6X3H])](=[!#6])[!#6]</term></item>
    /// <item><term>88</term><term>Carbothioic acid</term><term>[CX3;!R;$([C][#6]),$([CH]);$([C](=[OX1])[$([SX2H]),$([SX1-])]),$([C](=[SX1])[$([OX2H]),$([OX1-])])]</term></item>
    /// <item><term>89</term><term>Carbothioic S ester</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[SX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>90</term><term>Carbothioic S lactone</term><term>[#6][#6X3R](=[OX1])[#16X2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>91</term><term>Carbothioic O ester</term><term>[CX3;$([H0][#6]),$([H1])](=[SX1])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>92</term><term>Carbothioic O lactone</term><term>[#6][#6X3R](=[SX1])[#8X2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>93</term><term>Carbothioic halide</term><term>[CX3;$([H0][#6]),$([H1])](=[SX1])[FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>94</term><term>Carbodithioic acid</term><term>[CX3;!R;$([C][#6]),$([CH]);$([C](=[SX1])[SX2H])]</term></item>
    /// <item><term>95</term><term>Carbodithioic ester</term><term>[CX3;!R;$([C][#6]),$([CH]);$([C](=[SX1])[SX2][#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>96</term><term>Carbodithiolactone</term><term>[#6][#6X3R](=[SX1])[#16X2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>97</term><term>Amide</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>98</term><term>Primary amide</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[NX3H2]</term></item>
    /// <item><term>99</term><term>Secondary amide</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[#7X3H1][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>100</term><term>Tertiary amide</term><term>[CX3;$([R0][#6]),$([H1R0])](=[OX1])[#7X3H0]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>101</term><term>Lactam</term><term>[#6R][#6X3R](=[OX1])[#7X3;$([H1][#6;!$(C=[O,N,S])]),$([H0]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>102</term><term>Alkyl imide</term><term>[#6X3;$([H0][#6]),$([H1])](=[OX1])[#7X3H0]([#6])[#6X3;$([H0][#6]),$([H1])](=[OX1])</term></item>
    /// <item><term>103</term><term>N hetero imide</term><term>[#6X3;$([H0][#6]),$([H1])](=[OX1])[#7X3H0]([!#6])[#6X3;$([H0][#6]),$([H1])](=[OX1])</term></item>
    /// <item><term>104</term><term>Imide acidic</term><term>[#6X3;$([H0][#6]),$([H1])](=[OX1])[#7X3H1][#6X3;$([H0][#6]),$([H1])](=[OX1])</term></item>
    /// <item><term>105</term><term>Thioamide</term><term>[$([CX3;!R][#6]),$([CX3H;!R])](=[SX1])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>106</term><term>Thiolactam</term><term>[#6R][#6X3R](=[SX1])[#7X3;$([H1][#6;!$(C=[O,N,S])]),$([H0]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>107</term><term>Oximester</term><term>[#6X3;$([H0][#6]),$([H1])](=[OX1])[#8X2][#7X2]=,:[#6X3;$([H0]([#6])[#6]),$([H1][#6]),$([H2])]</term></item>
    /// <item><term>108</term><term>Amidine</term><term>[NX3;!$(NC=[O,S])][CX3;$([CH]),$([C][#6])]=[NX2;!$(NC=[O,S])]</term></item>
    /// <item><term>109</term><term>Hydroxamic acid</term><term>[CX3;$([H0][#6]),$([H1])](=[OX1])[#7X3;$([H1]),$([H0][#6;!$(C=[O,N,S])])][$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>110</term><term>Hydroxamic acid ester</term><term>[CX3;$([H0][#6]),$([H1])](=[OX1])[#7X3;$([H1]),$([H0][#6;!$(C=[O,N,S])])][OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>111</term><term>Imidoacid</term><term>[CX3R0;$([H0][#6]),$([H1])](=[NX2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>112</term><term>Imidoacid cyclic</term><term>[#6R][#6X3R](=,:[#7X2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>113</term><term>Imidoester</term><term>[CX3R0;$([H0][#6]),$([H1])](=[NX2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>114</term><term>Imidolactone</term><term>[#6R][#6X3R](=,:[#7X2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>115</term><term>Imidothioacid</term><term>[CX3R0;$([H0][#6]),$([H1])](=[NX2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[$([SX2H]),$([SX1-])]</term></item>
    /// <item><term>116</term><term>Imidothioacid cyclic</term><term>[#6R][#6X3R](=,:[#7X2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[$([SX2H]),$([SX1-])]</term></item>
    /// <item><term>117</term><term>Imidothioester</term><term>[CX3R0;$([H0][#6]),$([H1])](=[NX2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[SX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>118</term><term>Imidothiolactone</term><term>[#6R][#6X3R](=,:[#7X2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[SX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>119</term><term>Amidine</term><term>[#7X3v3;!$(N([#6X3]=[#7X2])C=[O,S])][CX3R0;$([H1]),$([H0][#6])]=[NX2v3;!$(N(=[#6X3][#7X3])C=[O,S])]</term></item>
    /// <item><term>120</term><term>Imidolactam</term><term>[#6][#6X3R;$([H0](=[NX2;!$(N(=[#6X3][#7X3])C=[O,S])])[#7X3;!$(N([#6X3]=[#7X2])C=[O,S])]),$([H0](-[NX3;!$(N([#6X3]=[#7X2])C=[O,S])])=,:[#7X2;!$(N(=[#6X3][#7X3])C=[O,S])])]</term></item>
    /// <item><term>121</term><term>Imidoylhalide</term><term>[CX3R0;$([H0][#6]),$([H1])](=[NX2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>122</term><term>Imidoylhalide cyclic</term><term>[#6R][#6X3R](=,:[#7X2;$([H1]),$([H0][#6;!$(C=[O,N,S])])])[FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>123</term><term>Amidrazone</term><term>[$([$([#6X3][#6]),$([#6X3H])](=[#7X2v3])[#7X3v3][#7X3v3]),$([$([#6X3][#6]),$([#6X3H])]([#7X3v3])=[#7X2v3][#7X3v3])]</term></item>
    /// <item><term>124</term><term>Alpha aminoacid</term><term>[NX3,NX4+;!$([N]~[!#6]);!$([N]*~[#7,#8,#15,#16])][C][CX3](=[OX1])[OX2H,OX1-]</term></item>
    /// <item><term>125</term><term>Alpha hydroxyacid</term><term>[OX2H][C][CX3](=[OX1])[OX2H,OX1-]</term></item>
    /// <item><term>126</term><term>Peptide middle</term><term>[NX3;$([N][CX3](=[OX1])[C][NX3,NX4+])][C][CX3](=[OX1])[NX3;$([N][C][CX3](=[OX1])[NX3,OX2,OX1-])]</term></item>
    /// <item><term>127</term><term>Peptide C term</term><term>[NX3;$([N][CX3](=[OX1])[C][NX3,NX4+])][C][CX3](=[OX1])[OX2H,OX1-]</term></item>
    /// <item><term>128</term><term>Peptide N term</term><term>[NX3,NX4+;!$([N]~[!#6]);!$([N]*~[#7,#8,#15,#16])][C][CX3](=[OX1])[NX3;$([N][C][CX3](=[OX1])[NX3,OX2,OX1-])]</term></item>
    /// <item><term>129</term><term>Carboxylic orthoester</term><term>[#6][OX2][CX4;$(C[#6]),$([CH])]([OX2][#6])[OX2][#6]</term></item>
    /// <item><term>130</term><term>Ketene</term><term>[CX3]=[CX2]=[OX1]</term></item>
    /// <item><term>131</term><term>Ketenacetal</term><term>[#7X2,#8X3,#16X2;$(*[#6,#14])][#6X3]([#7X2,#8X3,#16X2;$(*[#6,#14])])=[#6X3]</term></item>
    /// <item><term>132</term><term>Nitrile</term><term>[NX1]#[CX2]</term></item>
    /// <item><term>133</term><term>Isonitrile</term><term>[CX1-]#[NX2+]</term></item>
    /// <item><term>134</term><term>Vinylogous carbonyl or carboxyl derivative</term><term>[#6X3](=[OX1])[#6X3]=,:[#6X3][#7,#8,#16,F,Cl,Br,I]</term></item>
    /// <item><term>135</term><term>Vinylogous acid</term><term>[#6X3](=[OX1])[#6X3]=,:[#6X3][$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>136</term><term>Vinylogous ester</term><term>[#6X3](=[OX1])[#6X3]=,:[#6X3][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>137</term><term>Vinylogous amide</term><term>[#6X3](=[OX1])[#6X3]=,:[#6X3][#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>138</term><term>Vinylogous halide</term><term>[#6X3](=[OX1])[#6X3]=,:[#6X3][FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>139</term><term>Carbonic acid dieester</term><term>[#6;!$(C=[O,N,S])][#8X2][#6X3](=[OX1])[#8X2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>140</term><term>Carbonic acid esterhalide</term><term>[#6;!$(C=[O,N,S])][OX2;!R][CX3](=[OX1])[OX2][FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>141</term><term>Carbonic acid monoester</term><term>[#6;!$(C=[O,N,S])][OX2;!R][CX3](=[OX1])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>142</term><term>Carbonic acid derivatives</term><term>[!#6][#6X3](=[!#6])[!#6]</term></item>
    /// <item><term>143</term><term>Thiocarbonic acid dieester</term><term>[#6;!$(C=[O,N,S])][#8X2][#6X3](=[SX1])[#8X2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>144</term><term>Thiocarbonic acid esterhalide</term><term>[#6;!$(C=[O,N,S])][OX2;!R][CX3](=[SX1])[OX2][FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>145</term><term>Thiocarbonic acid monoester</term><term>[#6;!$(C=[O,N,S])][OX2;!R][CX3](=[SX1])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>146</term><term>Urea</term><term>[#7X3;!$([#7][!#6])][#6X3](=[OX1])[#7X3;!$([#7][!#6])]</term></item>
    /// <item><term>147</term><term>Thiourea</term><term>[#7X3;!$([#7][!#6])][#6X3](=[SX1])[#7X3;!$([#7][!#6])]</term></item>
    /// <item><term>148</term><term>Isourea</term><term>[#7X2;!$([#7][!#6])]=,:[#6X3]([#8X2&amp;!$([#8][!#6]),OX1-])[#7X3;!$([#7][!#6])]</term></item>
    /// <item><term>149</term><term>Isothiourea</term><term>[#7X2;!$([#7][!#6])]=,:[#6X3]([#16X2&amp;!$([#16][!#6]),SX1-])[#7X3;!$([#7][!#6])]</term></item>
    /// <item><term>150</term><term>Guanidine</term><term>[N;v3X3,v4X4+][CX3](=[N;v3X2,v4X3+])[N;v3X3,v4X4+]</term></item>
    /// <item><term>151</term><term>Carbaminic acid</term><term>[NX3]C(=[OX1])[O;X2H,X1-]</term></item>
    /// <item><term>152</term><term>Urethan</term><term>[#7X3][#6](=[OX1])[#8X2][#6]</term></item>
    /// <item><term>153</term><term>Biuret</term><term>[#7X3][#6](=[OX1])[#7X3][#6](=[OX1])[#7X3]</term></item>
    /// <item><term>154</term><term>Semicarbazide</term><term>[#7X3][#7X3][#6X3]([#7X3;!$([#7][#7])])=[OX1]</term></item>
    /// <item><term>155</term><term>Carbazide</term><term>[#7X3][#7X3][#6X3]([#7X3][#7X3])=[OX1]</term></item>
    /// <item><term>156</term><term>Semicarbazone</term><term>[#7X2](=[#6])[#7X3][#6X3]([#7X3;!$([#7][#7])])=[OX1]</term></item>
    /// <item><term>157</term><term>Carbazone</term><term>[#7X2](=[#6])[#7X3][#6X3]([#7X3][#7X3])=[OX1]</term></item>
    /// <item><term>158</term><term>Thiosemicarbazide</term><term>[#7X3][#7X3][#6X3]([#7X3;!$([#7][#7])])=[SX1]</term></item>
    /// <item><term>159</term><term>Thiocarbazide</term><term>[#7X3][#7X3][#6X3]([#7X3][#7X3])=[SX1]</term></item>
    /// <item><term>160</term><term>Thiosemicarbazone</term><term>[#7X2](=[#6])[#7X3][#6X3]([#7X3;!$([#7][#7])])=[SX1]</term></item>
    /// <item><term>161</term><term>Thiocarbazone</term><term>[#7X2](=[#6])[#7X3][#6X3]([#7X3][#7X3])=[SX1]</term></item>
    /// <item><term>162</term><term>Isocyanate</term><term>[NX2]=[CX2]=[OX1]</term></item>
    /// <item><term>163</term><term>Cyanate</term><term>[OX2][CX2]#[NX1]</term></item>
    /// <item><term>164</term><term>Isothiocyanate</term><term>[NX2]=[CX2]=[SX1]</term></item>
    /// <item><term>165</term><term>Thiocyanate</term><term>[SX2][CX2]#[NX1]</term></item>
    /// <item><term>166</term><term>Carbodiimide</term><term>[NX2]=[CX2]=[NX2]</term></item>
    /// <item><term>167</term><term>Orthocarbonic derivatives</term><term>[CX4H0]([O,S,#7])([O,S,#7])([O,S,#7])[O,S,#7,F,Cl,Br,I]</term></item>
    /// <item><term>168</term><term>Phenol</term><term>[OX2H][c]</term></item>
    /// <item><term>169</term><term>1,2-Diphenol</term><term>[OX2H][c][c][OX2H]</term></item>
    /// <item><term>170</term><term>Arylchloride</term><term>[Cl][c]</term></item>
    /// <item><term>171</term><term>Arylfluoride</term><term>[F][c]</term></item>
    /// <item><term>172</term><term>Arylbromide</term><term>[Br][c]</term></item>
    /// <item><term>173</term><term>Aryliodide</term><term>[I][c]</term></item>
    /// <item><term>174</term><term>Arylthiol</term><term>[SX2H][c]</term></item>
    /// <item><term>175</term><term>Iminoarene</term><term>[c]=[NX2;$([H1]),$([H0][#6;!$([C]=[N,S,O])])]</term></item>
    /// <item><term>176</term><term>Oxoarene</term><term>[c]=[OX1]</term></item>
    /// <item><term>177</term><term>Thioarene</term><term>[c]=[SX1]</term></item>
    /// <item><term>178</term><term>Hetero N basic H</term><term>[nX3H1+0]</term></item>
    /// <item><term>179</term><term>Hetero N basic no H</term><term>[nX3H0+0]</term></item>
    /// <item><term>180</term><term>Hetero N nonbasic</term><term>[nX2,nX3+]</term></item>
    /// <item><term>181</term><term>Hetero O</term><term>[o]</term></item>
    /// <item><term>182</term><term>Hetero S</term><term>[sX2]</term></item>
    /// <item><term>183</term><term>Heteroaromatic</term><term>[a;!c]</term></item>
    /// <item><term>184</term><term>Nitrite</term><term>[NX2](=[OX1])[O;$([X2]),$([X1-])]</term></item>
    /// <item><term>185</term><term>Thionitrite</term><term>[SX2][NX2]=[OX1]</term></item>
    /// <item><term>186</term><term>Nitrate</term><term>[$([NX3](=[OX1])(=[OX1])[O;$([X2]),$([X1-])]),$([NX3+]([OX1-])(=[OX1])[O;$([X2]),$([X1-])])]</term></item>
    /// <item><term>187</term><term>Nitro</term><term>[$([NX3](=O)=O),$([NX3+](=O)[O-])][!#8]</term></item>
    /// <item><term>188</term><term>Nitroso</term><term>[NX2](=[OX1])[!#7;!#8]</term></item>
    /// <item><term>189</term><term>Azide</term><term>[NX1]~[NX2]~[NX2,NX1]</term></item>
    /// <item><term>190</term><term>Acylazide</term><term>[CX3](=[OX1])[NX2]~[NX2]~[NX1]</term></item>
    /// <item><term>191</term><term>Diazo</term><term>[$([#6]=[NX2+]=[NX1-]),$([#6-]-[NX2+]#[NX1])]</term></item>
    /// <item><term>192</term><term>Diazonium</term><term>[#6][NX2+]#[NX1]</term></item>
    /// <item><term>193</term><term>Nitrosamine</term><term>[#7;!$(N*=O)][NX2]=[OX1]</term></item>
    /// <item><term>194</term><term>Nitrosamide</term><term>[NX2](=[OX1])N-*=O</term></item>
    /// <item><term>195</term><term>N-Oxide</term><term>[$([#7+][OX1-]),$([#7v5]=[OX1]);!$([#7](~[O])~[O]);!$([#7]=[#7])]</term></item>
    /// <item><term>196</term><term>Hydrazine</term><term>[NX3;$([H2]),$([H1][#6]),$([H0]([#6])[#6]);!$(NC=[O,N,S])][NX3;$([H2]),$([H1][#6]),$([H0]([#6])[#6]);!$(NC=[O,N,S])]</term></item>
    /// <item><term>197</term><term>Hydrazone</term><term>[NX3;$([H2]),$([H1][#6]),$([H0]([#6])[#6]);!$(NC=[O,N,S])][NX2]=[#6]</term></item>
    /// <item><term>198</term><term>Hydroxylamine</term><term>[NX3;$([H2]),$([H1][#6]),$([H0]([#6])[#6]);!$(NC=[O,N,S])][OX2;$([H1]),$(O[#6;!$(C=[N,O,S])])]</term></item>
    /// <item><term>199</term><term>Sulfon</term><term>[$([SX4](=[OX1])(=[OX1])([#6])[#6]),$([SX4+2]([OX1-])([OX1-])([#6])[#6])]</term></item>
    /// <item><term>200</term><term>Sulfoxide</term><term>[$([SX3](=[OX1])([#6])[#6]),$([SX3+]([OX1-])([#6])[#6])]</term></item>
    /// <item><term>201</term><term>Sulfonium</term><term>[S+;!$([S]~[!#6]);!$([S]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>202</term><term>Sulfuric acid</term><term>[SX4](=[OX1])(=[OX1])([$([OX2H]),$([OX1-])])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>203</term><term>Sulfuric monoester</term><term>[SX4](=[OX1])(=[OX1])([$([OX2H]),$([OX1-])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>204</term><term>Sulfuric diester</term><term>[SX4](=[OX1])(=[OX1])([OX2][#6;!$(C=[O,N,S])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>205</term><term>Sulfuric monoamide</term><term>[SX4](=[OX1])(=[OX1])([#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>206</term><term>Sulfuric diamide</term><term>[SX4](=[OX1])(=[OX1])([#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>207</term><term>Sulfuric esteramide</term><term>[SX4](=[OX1])(=[OX1])([#7X3][#6;!$(C=[O,N,S])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>208</term><term>Sulfuric derivative</term><term>[SX4D4](=[!#6])(=[!#6])([!#6])[!#6]</term></item>
    /// <item><term>209</term><term>Sulfonic acid</term><term>[SX4;$([H1]),$([H0][#6])](=[OX1])(=[OX1])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>210</term><term>Sulfonamide</term><term>[SX4;$([H1]),$([H0][#6])](=[OX1])(=[OX1])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>211</term><term>Sulfonic ester</term><term>[SX4;$([H1]),$([H0][#6])](=[OX1])(=[OX1])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>212</term><term>Sulfonic halide</term><term>[SX4;$([H1]),$([H0][#6])](=[OX1])(=[OX1])[FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>213</term><term>Sulfonic derivative</term><term>[SX4;$([H1]),$([H0][#6])](=[!#6])(=[!#6])[!#6]</term></item>
    /// <item><term>214</term><term>Sulfinic acid</term><term>[SX3;$([H1]),$([H0][#6])](=[OX1])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>215</term><term>Sulfinic amide</term><term>[SX3;$([H1]),$([H0][#6])](=[OX1])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>216</term><term>Sulfinic ester</term><term>[SX3;$([H1]),$([H0][#6])](=[OX1])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>217</term><term>Sulfinic halide</term><term>[SX3;$([H1]),$([H0][#6])](=[OX1])[FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>218</term><term>Sulfinic derivative</term><term>[SX3;$([H1]),$([H0][#6])](=[!#6])[!#6]</term></item>
    /// <item><term>219</term><term>Sulfenic acid</term><term>[SX2;$([H1]),$([H0][#6])][$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>220</term><term>Sulfenic amide</term><term>[SX2;$([H1]),$([H0][#6])][#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>221</term><term>Sulfenic ester</term><term>[SX2;$([H1]),$([H0][#6])][OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>222</term><term>Sulfenic halide</term><term>[SX2;$([H1]),$([H0][#6])][FX1,ClX1,BrX1,IX1]</term></item>
    /// <item><term>223</term><term>Sulfenic derivative</term><term>[SX2;$([H1]),$([H0][#6])][!#6]</term></item>
    /// <item><term>224</term><term>Phosphine</term><term>[PX3;$([H3]),$([H2][#6]),$([H1]([#6])[#6]),$([H0]([#6])([#6])[#6])]</term></item>
    /// <item><term>225</term><term>Phosphine oxide</term><term>[PX4;$([H3]=[OX1]),$([H2](=[OX1])[#6]),$([H1](=[OX1])([#6])[#6]),$([H0](=[OX1])([#6])([#6])[#6])]</term></item>
    /// <item><term>226</term><term>Phosphonium</term><term>[P+;!$([P]~[!#6]);!$([P]*~[#7,#8,#15,#16])]</term></item>
    /// <item><term>227</term><term>Phosphorylen</term><term>[PX4;$([H3]=[CX3]),$([H2](=[CX3])[#6]),$([H1](=[CX3])([#6])[#6]),$([H0](=[CX3])([#6])([#6])[#6])]</term></item>
    /// <item><term>228</term><term>Phosphonic acid</term><term>[PX4;$([H1]),$([H0][#6])](=[OX1])([$([OX2H]),$([OX1-])])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>229</term><term>Phosphonic monoester</term><term>[PX4;$([H1]),$([H0][#6])](=[OX1])([$([OX2H]),$([OX1-])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>230</term><term>Phosphonic diester</term><term>[PX4;$([H1]),$([H0][#6])](=[OX1])([OX2][#6;!$(C=[O,N,S])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>231</term><term>Phosphonic monoamide</term><term>[PX4;$([H1]),$([H0][#6])](=[OX1])([$([OX2H]),$([OX1-])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>232</term><term>Phosphonic diamide</term><term>[PX4;$([H1]),$([H0][#6])](=[OX1])([#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>233</term><term>Phosphonic esteramide</term><term>[PX4;$([H1]),$([H0][#6])](=[OX1])([OX2][#6;!$(C=[O,N,S])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>234</term><term>Phosphonic acid derivative</term><term>[PX4;$([H1]),$([H0][#6])](=[!#6])([!#6])[!#6]</term></item>
    /// <item><term>235</term><term>Phosphoric acid</term><term>[PX4D4](=[OX1])([$([OX2H]),$([OX1-])])([$([OX2H]),$([OX1-])])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>236</term><term>Phosphoric monoester</term><term>[PX4D4](=[OX1])([$([OX2H]),$([OX1-])])([$([OX2H]),$([OX1-])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>237</term><term>Phosphoric diester</term><term>[PX4D4](=[OX1])([$([OX2H]),$([OX1-])])([OX2][#6;!$(C=[O,N,S])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>238</term><term>Phosphoric triester</term><term>[PX4D4](=[OX1])([OX2][#6;!$(C=[O,N,S])])([OX2][#6;!$(C=[O,N,S])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>239</term><term>Phosphoric monoamide</term><term>[PX4D4](=[OX1])([$([OX2H]),$([OX1-])])([$([OX2H]),$([OX1-])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>240</term><term>Phosphoric diamide</term><term>[PX4D4](=[OX1])([$([OX2H]),$([OX1-])])([#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>241</term><term>Phosphoric triamide</term><term>[PX4D4](=[OX1])([#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])])([#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>242</term><term>Phosphoric monoestermonoamide</term><term>[PX4D4](=[OX1])([$([OX2H]),$([OX1-])])([OX2][#6;!$(C=[O,N,S])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>243</term><term>Phosphoric diestermonoamide</term><term>[PX4D4](=[OX1])([OX2][#6;!$(C=[O,N,S])])([OX2][#6;!$(C=[O,N,S])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>244</term><term>Phosphoric monoesterdiamide</term><term>[PX4D4](=[OX1])([OX2][#6;!$(C=[O,N,S])])([#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>245</term><term>Phosphoric acid derivative</term><term>[PX4D4](=[!#6])([!#6])([!#6])[!#6]</term></item>
    /// <item><term>246</term><term>Phosphinic acid</term><term>[PX4;$([H2]),$([H1][#6]),$([H0]([#6])[#6])](=[OX1])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>247</term><term>Phosphinic ester</term><term>[PX4;$([H2]),$([H1][#6]),$([H0]([#6])[#6])](=[OX1])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>248</term><term>Phosphinic amide</term><term>[PX4;$([H2]),$([H1][#6]),$([H0]([#6])[#6])](=[OX1])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>249</term><term>Phosphinic acid derivative</term><term>[PX4;$([H2]),$([H1][#6]),$([H0]([#6])[#6])](=[!#6])[!#6]</term></item>
    /// <item><term>250</term><term>Phosphonous acid</term><term>[PX3;$([H1]),$([H0][#6])]([$([OX2H]),$([OX1-])])[$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>251</term><term>Phosphonous monoester</term><term>[PX3;$([H1]),$([H0][#6])]([$([OX2H]),$([OX1-])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>252</term><term>Phosphonous diester</term><term>[PX3;$([H1]),$([H0][#6])]([OX2][#6;!$(C=[O,N,S])])[OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>253</term><term>Phosphonous monoamide</term><term>[PX3;$([H1]),$([H0][#6])]([$([OX2H]),$([OX1-])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>254</term><term>Phosphonous diamide</term><term>[PX3;$([H1]),$([H0][#6])]([#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>255</term><term>Phosphonous esteramide</term><term>[PX3;$([H1]),$([H0][#6])]([OX2][#6;!$(C=[O,N,S])])[#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>256</term><term>Phosphonous derivatives</term><term>[PX3;$([D2]),$([D3][#6])]([!#6])[!#6]</term></item>
    /// <item><term>257</term><term>Phosphinous acid</term><term>[PX3;$([H2]),$([H1][#6]),$([H0]([#6])[#6])][$([OX2H]),$([OX1-])]</term></item>
    /// <item><term>258</term><term>Phosphinous ester</term><term>[PX3;$([H2]),$([H1][#6]),$([H0]([#6])[#6])][OX2][#6;!$(C=[O,N,S])]</term></item>
    /// <item><term>259</term><term>Phosphinous amide</term><term>[PX3;$([H2]),$([H1][#6]),$([H0]([#6])[#6])][#7X3;$([H2]),$([H1][#6;!$(C=[O,N,S])]),$([#7]([#6;!$(C=[O,N,S])])[#6;!$(C=[O,N,S])])]</term></item>
    /// <item><term>260</term><term>Phosphinous derivatives</term><term>[PX3;$([H2]),$([H1][#6]),$([H0]([#6])[#6])][!#6]</term></item>
    /// <item><term>261</term><term>Quart silane</term><term>[SiX4]([#6])([#6])([#6])[#6]</term></item>
    /// <item><term>262</term><term>Non-quart silane</term><term>[SiX4;$([H1]([#6])([#6])[#6]),$([H2]([#6])[#6]),$([H3][#6]),$([H4])]</term></item>
    /// <item><term>263</term><term>Silylmonohalide</term><term>[SiX4]([FX1,ClX1,BrX1,IX1])([#6])([#6])[#6]</term></item>
    /// <item><term>264</term><term>Het trialkylsilane</term><term>[SiX4]([!#6])([#6])([#6])[#6]</term></item>
    /// <item><term>265</term><term>Dihet dialkylsilane</term><term>[SiX4]([!#6])([!#6])([#6])[#6]</term></item>
    /// <item><term>266</term><term>Trihet alkylsilane</term><term>[SiX4]([!#6])([!#6])([!#6])[#6]</term></item>
    /// <item><term>267</term><term>Silicic acid derivative</term><term>[SiX4]([!#6])([!#6])([!#6])[!#6]</term></item>
    /// <item><term>268</term><term>Trialkylborane</term><term>[BX3]([#6])([#6])[#6]</term></item>
    /// <item><term>269</term><term>Boric acid derivatives</term><term>[BX3]([!#6])([!#6])[!#6]</term></item>
    /// <item><term>270</term><term>Boronic acid derivative</term><term>[BX3]([!#6])([!#6])[!#6]</term></item>
    /// <item><term>271</term><term>Borohydride</term><term>[BH1,BH2,BH3,BH4]</term></item>
    /// <item><term>272</term><term>Quaternary boron</term><term>[BX4]</term></item>
    /// <item><term>273</term><term>Aromatic</term><term>a</term></item>
    /// <item><term>274</term><term>Heterocyclic</term><term>[!#6;!R0]</term></item>
    /// <item><term>275</term><term>Epoxide</term><term>[OX2r3]1[#6r3][#6r3]1</term></item>
    /// <item><term>276</term><term>NH aziridine</term><term>[NX3H1r3]1[#6r3][#6r3]1</term></item>
    /// <item><term>277</term><term>Spiro</term><term>[D4R;$(*(@*)(@*)(@*)@*)]</term></item>
    /// <item><term>278</term><term>Annelated rings</term><term>[R;$(*(@*)(@*)@*);!$([R2;$(*(@*)(@*)(@*)@*)])]@[R;$(*(@*)(@*)@*);!$([R2;$(*(@*)(@*)(@*)@*)])]</term></item>
    /// <item><term>279</term><term>Bridged rings</term><term>[R;$(*(@*)(@*)@*);!$([D4R;$(*(@*)(@*)(@*)@*)]);!$([R;$(*(@*)(@*)@*);!$([R2;$(*(@*)(@*)(@*)@*)])]@[R;$(*(@*)(@*)@*);!$([R2;$(*(@*)(@*)(@*)@*)])])]</term></item>
    /// <item><term>280</term><term>Sugar pattern 1</term><term>[OX2;$([r5]1@C@C@C(O)@C1),$([r6]1@C@C@C(O)@C(O)@C1)]</term></item>
    /// <item><term>281</term><term>Sugar pattern 2</term><term>[OX2;$([r5]1@C(!@[OX2,NX3,SX2,FX1,ClX1,BrX1,IX1])@C@C@C1),$([r6]1@C(!@[OX2,NX3,SX2,FX1,ClX1,BrX1,IX1])@C@C@C@C1)]</term></item>
    /// <item><term>282</term><term>Sugar pattern combi</term><term>[OX2;$([r5]1@C(!@[OX2,NX3,SX2,FX1,ClX1,BrX1,IX1])@C@C(O)@C1),$([r6]1@C(!@[OX2,NX3,SX2,FX1,ClX1,BrX1,IX1])@C@C(O)@C(O)@C1)]</term></item>
    /// <item><term>283</term><term>Sugar pattern 2 reducing</term><term>[OX2;$([r5]1@C(!@[OX2H1])@C@C@C1),$([r6]1@C(!@[OX2H1])@C@C@C@C1)]</term></item>
    /// <item><term>284</term><term>Sugar pattern 2 alpha</term><term>[OX2;$([r5]1@[C@@](!@[OX2,NX3,SX2,FX1,ClX1,BrX1,IX1])@C@C@C1),$([r6]1@[C@@](!@[OX2,NX3,SX2,FX1,ClX1,BrX1,IX1])@C@C@C@C1)]</term></item>
    /// <item><term>285</term><term>Sugar pattern 2 beta</term><term>[OX2;$([r5]1@[C@](!@[OX2,NX3,SX2,FX1,ClX1,BrX1,IX1])@C@C@C1),$([r6]1@[C@](!@[OX2,NX3,SX2,FX1,ClX1,BrX1,IX1])@C@C@C@C1)]</term></item>
    /// <item><term>286</term><term>Conjugated double bond</term><term>*=*[*]=,#,:[*]</term></item>
    /// <item><term>287</term><term>Conjugated tripple bond</term><term>*#*[*]=,#,:[*]</term></item>
    /// <item><term>288</term><term>Cis double bond</term><term>*&amp;#47[D2]=[D2]/*</term></item>
    /// <item><term>289</term><term>Trans double bond</term><term>*&amp;#47[D2]=[D2]/*</term></item>
    /// <item><term>290</term><term>Mixed anhydrides</term><term>[$(*=O),$([#16,#14,#5]),$([#7]([#6]=[OX1]))][#8X2][$(*=O),$([#16,#14,#5]),$([#7]([#6]=[OX1]))]</term></item>
    /// <item><term>291</term><term>Halogen on hetero</term><term>[FX1,ClX1,BrX1,IX1][!#6]</term></item>
    /// <item><term>292</term><term>Halogen multi subst</term><term>[F,Cl,Br,I;!$([X1]);!$([X0-])]</term></item>
    /// <item><term>293</term><term>Trifluoromethyl</term><term>[FX1][CX4;!$([H0][Cl,Br,I]);!$([F][C]([F])([F])[F])]([FX1])([FX1])</term></item>
    /// <item><term>294</term><term>C ONS bond</term><term>[#6]~[#7,#8,#16]</term></item>
    /// <item><term>295</term><term>Charged</term><term>[!+0]</term></item>
    /// <item><term>296</term><term>Anion</term><term>[-1,-2,-3,-4,-5,-6,-7]</term></item>
    /// <item><term>297</term><term>Kation</term><term>[+1,+2,+3,+4,+5,+6,+7]</term></item>
    /// <item><term>298</term><term>Salt</term><term>([-1,-2,-3,-4,-5,-6,-7]).([+1,+2,+3,+4,+5,+6,+7])</term></item>
    /// <item><term>299</term><term>1,3-Tautomerizable</term><term>[$([#7X2,OX1,SX1]=*[!H0;!$([a;!n])]),$([#7X3,OX2,SX2;!H0]*=*),$([#7X3,OX2,SX2;!H0]*:n)]</term></item>
    /// <item><term>300</term><term>1,5-Tautomerizable</term><term>[$([#7X2,OX1,SX1]=,:**=,:*[!H0;!$([a;!n])]),$([#7X3,OX2,SX2;!H0]*=**=*),$([#7X3,OX2,SX2;!H0]*=,:**:n)]</term></item>
    /// <item><term>301</term><term>Rotatable bond</term><term>[!$(*#*)&amp;!D1]-!@[!$(*#*)&amp;!D1]</term></item>
    /// <item><term>302</term><term>Michael acceptor</term><term>[CX3]=[CX3][$([CX3]=[O,N,S]),$(C#[N]),$([S,P]=[OX1]),$([NX3]=O),$([NX3+](=O)[O-])]</term></item>
    /// <item><term>303</term><term>Dicarbodiazene</term><term>[CX3](=[OX1])[NX2]=[NX2][CX3](=[OX1])</term></item>
    /// <item><term>304</term><term>CH-acidic</term><term>[$([CX4;!$([H0]);!$(C[!#6;!$([P,S]=O);!$(N(~O)~O)])][$([CX3]=[O,N,S]),$(C#[N]),$([S,P]=[OX1]),$([NX3]=O),$([NX3+](=O)[O-]);!$(*[S,O,N;H1,H2]);!$([*+0][S,O;X1-])]),$([CX4;!$([H0])]1[CX3]=[CX3][CX3]=[CX3]1)]</term></item>
    /// <item><term>305</term><term>CH-acidic strong</term><term>[CX4;!$([H0]);!$(C[!#6;!$([P,S]=O);!$(N(~O)~O)])]([$([CX3]=[O,N,S]),$(C#[N]),$([S,P]=[OX1]),$([NX3]=O),$([NX3+](=O)[O-]);!$(*[S,O,N;H1,H2]);!$([*+0][S,O;X1-])])[$([CX3]=[O,N,S]),$(C#[N]),$([S,P]=[OX1]),$([NX3]=O),$([NX3+](=O)[O-]);!$(*[S,O,N;H1,H2]);!$([*+0][S,O;X1-])]</term></item>
    /// <item><term>306</term><term>Chiral center specified</term><term>[$([*@](~*)(~*)(*)*),$([*@H](*)(*)*),$([*@](~*)(*)*),$([*@H](~*)~*)]</term></item>
    /// </tbody></list>
    // @author       egonw
    // @cdk.created  2005-12-30
    // @cdk.keyword  fingerprint
    // @cdk.keyword  similarity
    // @cdk.module   fingerprint
    public class SubstructureFingerprinter 
        : AbstractFingerprinter, IFingerprinter
    {
        private sealed class Key
        {
            public string Smarts { get; private set; }
            public Pattern Pattern { get; private set; }

            public Key(string smarts, Pattern pattern)
            {
                this.Smarts = smarts;
                this.Pattern = pattern;
            }
        }

        private readonly List<Key> keys = new List<Key>();

        /// <summary>
        /// Set up the fingerprinter to use a user-defined set of fragments.
        /// </summary>
        /// <param name="smarts">The collection of fragments to look for</param>
        public SubstructureFingerprinter(IEnumerable<string> smarts)
        {
            SetSmarts(smarts);
        }

        /// <summary>
        /// Set up the fingerprinter to use the fragments from <see cref="StandardSubstructureSets"/>.
        /// </summary>
        public SubstructureFingerprinter()
        {
            try
            {
                SetSmarts(StandardSubstructureSets.GetFunctionalGroupSMARTS());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not load SMARTS patterns", ex);
            }
        }

        /// <summary>
        /// Set the SMARTS patterns.
        /// </summary>
        /// <param name="smarts">the SMARTS</param>
        private void SetSmarts(IEnumerable<string> smarts)
        {
            keys.Clear();
            foreach (var key in smarts)
            {
                var qmol = new QueryAtomContainer();
                var ptrn = SmartsPattern.Create(key);
                ptrn.SetPrepare(false); // prepare is done once
                keys.Add(new Key(key, ptrn));
            }
        }

        /// <inheritdoc/>
        public override IBitFingerprint GetBitFingerprint(IAtomContainer atomContainer)
        {
            if (!keys.Any())
            {
                throw new CDKException("No substructures were defined");
            }

            SmartsPattern.Prepare(atomContainer);
            var fingerPrint = new BitArray(keys.Count);

            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Pattern.Matches(atomContainer))
                    fingerPrint[i] = true;
            }
            return new BitSetFingerprint(fingerPrint);
        }

        /// <inheritdoc/>
        public override ICountFingerprint GetCountFingerprint(IAtomContainer atomContainer)
        {
            if (!keys.Any())
            {
                throw new CDKException("No substructures were defined");
            }

            // init SMARTS invariants (connectivity, degree, etc)
            SmartsPattern.Prepare(atomContainer);

            var map = new SortedDictionary<int, int>();
            for (int i = 0; i < keys.Count; i++)
            {
                var ptrn = keys[i].Pattern;
                map[i] = ptrn.MatchAll(atomContainer).CountUnique();
            }
            return new CountFingerprint(map);
        }

        class CountFingerprint : ICountFingerprint
        {
            readonly IReadOnlyDictionary<int, int> map;
            readonly int[] keys;
            readonly int[] values;

            public CountFingerprint(IReadOnlyDictionary<int, int> map)
            {
                this.map = map;
                this.keys = map.Keys.ToArray();
                this.values = map.Values.ToArray();
            }

            public long Length => map.Count;
            public int GetCount(int index) => values[index];
            public int GetCountForHash(int hash) => map[hash];
            public int GetHash(int index) => keys[index];
            public int GetNumberOfPopulatedBins() => map.Count;
            public bool HasHash(int hash) => map.ContainsKey(hash);
            public void Merge(ICountFingerprint fp) { }
            public void SetBehaveAsBitFingerprint(bool behaveAsBitFingerprint) { }
        }

        /// <inheritdoc/>
        public override IReadOnlyDictionary<string, int> GetRawFingerprint(IAtomContainer mol)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int Length => keys.Count;

        /// <summary>
        /// Retrieves the SMARTS representation of a substructure for a given
        /// bit in the fingerprint.
        /// </summary>
        /// <param name="bitIndex"></param>
        /// <returns>SMARTS representation of substructure at index <paramref name="bitIndex"/>.</returns>
        public string GetSubstructure(int bitIndex)
        {
            return keys[bitIndex].Smarts;
        }
    }
}
