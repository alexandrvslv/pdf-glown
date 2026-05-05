#!/bin/bash
#
# Shell script to run PDF Glown samples on Mono.

(
# Insert a look-up reference to the PdfGlown library directory!
export MONO_PATH=MONO_PATH:`pwd`/../PdfGlown/build/package:`pwd`/../PdfGlown/lib
cp PdfGlownCLISamples.exe.config build/package
# Execute the test!
mono --debug ./build/package/PdfGlownCLISamples.exe #2> ../log/PdfGlownCLISamples.exe.log
)
