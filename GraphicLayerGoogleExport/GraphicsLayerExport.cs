/******************************************************************************
*   License:
*
*   Copyright (C) 2011 by Sky Peters
*
*   Permission is hereby granted, free of charge, to any person obtaining a copy
*   of this software and associated documentation files (the "Software"), to deal
*   in the Software without restriction, including without limitation the rights
*   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
*   copies of the Software, and to permit persons to whom the Software is
*   furnished to do so, subject to the following conditions:
*
*   The above copyright notice and this permission notice shall be included in
*   all copies or substantial portions of the Software.
*
*   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
*   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
*   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
*   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
*   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
*   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
*   THE SOFTWARE.
******************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Xml;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Symbols;
using ESRI.ArcGIS.Client.Toolkit.Primitives;
using Google.KML;
using ICSharpCode.SharpZipLib.Zip;


namespace GraphicLayerGoogleExport
{

  /// <summary>
  /// Provides interface implementation for export capability for GraphicsLayer
  /// </summary>
  public class GraphicsLayerExport
  {
    private GraphicsLayer mLayer;
    private List<Symbol> mUsedSymbols;

    /// <summary>
    /// Fires when export is complete
    /// </summary>
    public event EventHandler<ResponseEventArgs<byte[]>> ExportComplete;

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="overlay">Overlay this capability is assigned to</param>
    public GraphicsLayerExport(GraphicsLayer layer)
    {
      mLayer = layer;
      mUsedSymbols = new List<Symbol>();
      EnsureSymbolStyle();

    }

    /// <summary>
    /// This function creates a list of symbols that are used in the layer rendering.  
    /// Ensures styles etc. are not created twice for the same symbol in the KML output.
    /// </summary>
    private void EnsureSymbolStyle()
    {
      Symbol symbol = null;

      foreach(Graphic grph in mLayer.Graphics)
      {
        if(grph.Symbol != null)
        {
          symbol = grph.Symbol;
        }

        if(symbol != null)
        {
          if(!mUsedSymbols.Contains(symbol))
          {
            mUsedSymbols.Add(symbol);
          }
        }
      }

    }

    private List<ExportFile> GetImages()
    {
      List<ExportFile> images = new List<ExportFile>();

      foreach(Symbol symbol in mUsedSymbols)
      {
        SymbolDisplay symDisplay = new SymbolDisplay();

        symDisplay.Symbol = symbol;
        symDisplay.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        symDisplay.VerticalAlignment = System.Windows.VerticalAlignment.Center;

        //Need to put panel in popup and open popup, so panel ends up in control visual tree.
        //Otherwise when bitmap is rendered, panel will not include any child controls.        
        Popup popUp = new System.Windows.Controls.Primitives.Popup();
        popUp.Child = symDisplay;
        popUp.IsOpen = true;
        popUp.UpdateLayout();

        WriteableBitmap bmp = new WriteableBitmap(symDisplay, null);
        ImageTools.Image img = ImageTools.ImageExtensions.ToImage(bmp);

        MemoryStream stream = new MemoryStream();

        ImageTools.ImageExtensions.WriteToStream(img, stream);

        images.Add(new ExportFile()
        {
          Title = symbol.GetHashCode().ToString(),
          Path = string.Format("files/{0}.png", symbol.GetHashCode().ToString()),
          Data = stream.ToArray()
        });

        stream.Dispose();

        popUp.IsOpen = false;
      }

      return images;
    }

    private List<ExportFile> GenerateExportFiles()
    {
      KMLGraphicConverter converter = new KMLGraphicConverter();
      geFeature kmlFeature = converter.GetExportKML(mLayer.Graphics, mLayer.ID, mLayer.ID, mUsedSymbols);

      XmlWriterSettings settings = new XmlWriterSettings();
      settings.Indent = true;

      MemoryStream stream = new MemoryStream();
      StreamWriter writer = new StreamWriter(stream);
      XmlWriter xmlWriter = XmlWriter.Create(writer, settings);

      //Create kml file
      xmlWriter.WriteStartDocument();
      xmlWriter.WriteStartElement("kml");
      kmlFeature.ToKML(xmlWriter);
      xmlWriter.WriteEndElement();
      xmlWriter.WriteEndDocument();

      xmlWriter.Flush();
      xmlWriter.Close();

      List<ExportFile> files = new List<ExportFile>();

      files.Add(new ExportFile()
      {
        Title = mLayer.ID,
        Path = "doc.kml",
        Data = stream.ToArray()
      });

      files.AddRange(GetImages()); //create images from symbols

      return files;
    }

    public void ExportToKMZ()
    {
      List<ExportFile> exportedFiles = GenerateExportFiles();

      MemoryStream stream = new MemoryStream();
      ZipOutputStream zipStream = new ZipOutputStream(stream);

      //add exported files to kmz.
      for(int i = 0; i < exportedFiles.Count; i++)
      {
        byte[] fileBytes = exportedFiles[i].Data;

        ZipEntry entry = new ZipEntry(string.Format("{1}", i, exportedFiles[i].Path));
        entry.Size = fileBytes.Length;

        zipStream.SetLevel(9);
        zipStream.PutNextEntry(entry);
        zipStream.Write(fileBytes, 0, fileBytes.Length);
        zipStream.CloseEntry();
      }

      zipStream.Finish();
      zipStream.Close();

      ExportComplete(this, new ResponseEventArgs<byte[]>(stream.ToArray(), null));
    }
  }
}
