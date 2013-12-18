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
using System.Windows;
using System.Windows.Controls;
using ESRI.ArcGIS.Client;

namespace GraphicLayerGoogleExport
{
  public partial class MainPage : UserControl
  {
    public MainPage()
    {
      InitializeComponent();

      //Attributes must be set in code.  No XAML support for attribute creation.
      SetGraphicAttributes();
    }

    private void SetGraphicAttributes()
    {
      GraphicsLayer gl = myMap.Layers[1] as GraphicsLayer;


      gl.Graphics[0].Attributes.Add("id", 1);
      gl.Graphics[0].Attributes.Add("Name", "graphic1");
      gl.Graphics[0].Attributes.Add("Description", "this is graphic1");

      gl.Graphics[1].Attributes.Add("id", 2);
      gl.Graphics[1].Attributes.Add("Name", "graphic2");
      gl.Graphics[1].Attributes.Add("Description", "this is graphic2");

      gl.Graphics[2].Attributes.Add("id", 3);
      gl.Graphics[2].Attributes.Add("Name", "graphic3");
      gl.Graphics[2].Attributes.Add("Description", "this is graphic3");

      gl.Graphics[3].Attributes.Add("id", 4);
      gl.Graphics[3].Attributes.Add("Name", "graphic4");
      gl.Graphics[3].Attributes.Add("Description", "this is graphic4");

      gl.Graphics[4].Attributes.Add("id", 5);
      gl.Graphics[4].Attributes.Add("Name", "graphic5");
      gl.Graphics[4].Attributes.Add("Description", "this is graphic5");

      gl.Graphics[5].Attributes.Add("id", 6);
      gl.Graphics[5].Attributes.Add("Name", "graphic6");
      gl.Graphics[5].Attributes.Add("Description", "this is graphic6");

      gl.Graphics[6].Attributes.Add("id", 7);
      gl.Graphics[6].Attributes.Add("Name", "graphic7");
      gl.Graphics[6].Attributes.Add("Description", "this is graphic7");

    }



    private void Button_Click(object sender, RoutedEventArgs e)
    {
      SaveFileDialog sfd = new SaveFileDialog()
      {       
        DefaultExt = "kmz",
        Filter = ".kmz|*.kmz"
      };

      if(sfd.ShowDialog() == true)
      {
        try
        {
          GraphicsLayerExport exp = new GraphicsLayerExport(myMap.Layers[1] as GraphicsLayer);

          exp.ExportComplete += delegate(object exportSender,
                                         ResponseEventArgs<byte[]> args)
          {
            if(args.Error != null)
            {
              MessageBox.Show(args.Error.Message);
              return;
            }

            using(System.IO.Stream stream = sfd.OpenFile())
            {
              byte[] expBytes = args.Result;

              stream.Write(expBytes, 0, expBytes.Length);
              stream.Flush();
              stream.Close();
            }

          };

          exp.ExportToKMZ();
        }
        catch(Exception exc)
        {
          MessageBox.Show(exc.Message);
        }
      }
    }
  }
}
