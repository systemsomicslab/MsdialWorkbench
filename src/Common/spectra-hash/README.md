# SPLASH#

A C# implementation of the [SPLASH](http://splash.fiehnlab.ucdavis.edu) (SPectraL hASH), an unambiguous, database-independent spectral identifier.  

## Compilation

### Requirements:
  - Mono MDK (Download from: http://www.mono-project.com/download/)
  - (Optional) MonoDevelop IDE (Download from: http://www.monodevelop.com/download/)

### Building:
The easiest way to build the project is using MonoDevelop, open the IDE and load the solution (`<download folder>/csharp/splash.sln`).
On the 'Solution Explorer' (left panel), right click 'splash' and select 'Build splash', if there are no errors you will see a 'Build successful' message.


## Usage

To generate a splash you need to add a reference to the assembly `Splash.dll` to your project then add the following 'using' statement:
```
using NSSplash;
```

To get the hash for a given spectrum you can call:
```
Splash splasher = new Splash();
string hash = splasher.splashIt(new Spectrum("5.0000001:1.0 5.0000005:0.5 10.02395773287:2.0 11.234568:.10", SpectrumType.MS));
```

## Credits

This library was written by Diego Pedrosa is licensed under the [BSD 3 license](https://github.com/berlinguyinca/spectra-hash/blob/master/license).