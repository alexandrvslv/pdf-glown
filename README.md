# Pdf Glown - Skia Sharp
Fork of https://sourceforge.net/projects/clown/.

## Fork Task

- Pdf rendering by [SkiaSharp](https://github.com/mono/SkiaSharp).
- UI integrations 
  - [Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly?view=aspnetcore-8.0) (betta)
  - [Xamarin.Forms](https://github.com/xamarin/Xamarin.Forms) (expired)
  - [Avalonia UI](https://avaloniaui.net/) (alfa)
  - [Uno Platform](https://platform.uno/) (todo)
  - [MAUI](https://learn.microsoft.com/ru-ru/dotnet/maui/what-is-maui?view=net-maui-8.0) (todo)
- Competitive performance

## Status

- Successfully render Pdf on 'SkiaSharp.SKCanvas'
  - Basic painting reguired just replace System.Drawing by SkiaSharp, thanks to author Stefano Chizzolini
  - New mandatory features of SkiaSharp(for Blazor, Tiling, Image Mask, Gradient and Patch shaders) by [mattleibow](https://github.com/mattleibow)
  - XObject Masking by [warappa](https://github.com/warappa)

- Change Code amd Docs formatting
- Rendering and Editing Pdf Annotations
- Move core projects to .net8 (Blazor on .net9)
- Performance improvements
  - PdfName cached globbaly
  - Suppress reflections invocation
  - A lot of PdfObjectWrappers replaced by direct PdfObject enhiritance
  - Force use of Memory\<byte\>, Span\<byte\>
- Fonts, Encryption, Functions, Shadings by integrate [Apache PdfBox Project](https://pdfbox.apache.org/) from [mirror](https://github.com/apache/pdfbox).
  - Source code translated from java to C#
  - Full Fonts processing & text rendering engine
  - LZW, CCITTFax and other fixes of Images loading engine
  - Decryption
  - Signature Fields - basic models
  - Functions 0-4
  - Shaders 4, 5, 6
- Images and ColorSpaces by integrate [Mozilla Pdf.js](https://github.com/mozilla/pdf.js)
  - Source code translated from js to C#
  - JPX, CCITTFax, JBIG2 - decoding

## TODO
- Release Nuget packages (maybe PdfGlown.Skia, PdfGlown.Skia.UI.Core, PdfGlown.Skia.UI.Blazor)
- Rendering
  - Streaming decode filters and optimization
  - Alpha Masking won't work correctly after my refactoring	  
- Encryption.
  - Encryption not tested
  - Public key Certificat - requer completly rework PdfBox solution
  - Digital Signatures (MDS, DSS)

