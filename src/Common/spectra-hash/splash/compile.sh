#!/bin/bash

###
# very basic compilation script to create the Splash.dll and the SplashRunner.exe in the './out' dir
###

if [ ! -d "out" ]; then
	mkdir "out"
fi

# compile the library
dmcs -optimize -t:library -r:System.Numerics.dll -out:out/Splash.dll -recurse:'impl/*.cs' ISplash.cs ISpectrum.cs Splash.cs

# compile the runner
dmcs -optimize -t:exe -out:out/SplashRunner.exe -r:out/Splash.dll SplashRunner.cs StatisticBuilder.cs
