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
using System.Text;
using System.Windows.Media;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using Google.KML;


namespace GraphicLayerGoogleExport
{
  public class KMLGraphicConverter
  {
    private string mStyleName;
    private string mSchemaName;
    private IList<Graphic> mObjects;
    private string mFeatureName;
    private string mFeatureId;
    private List<Symbol> mUsedSymbols;


    public KMLGraphicConverter()
    {
    }

    public geFeature GetExportKML(IList<Graphic> Graphics,
                                  string featureName,
                                  string featureId,
                                  List<Symbol> usedSymbols)
    {
      geDocument dt;
      geStyle st;
      Random random;
      Color color;

      mFeatureName = featureName;
      mFeatureId = featureId;
      mObjects = Graphics;
      mUsedSymbols = usedSymbols;

      dt = new geDocument();

      dt.Name = mFeatureName;

      if(mObjects.Count > 0)
      {
        mStyleName = mFeatureId + "styleid";
        mSchemaName = mFeatureId + "schemaid";

        foreach(Graphic graphic in mObjects)
        {
          dt.Schemas.Add(CreateKMLPlacemarkSchema(graphic));

        }
        dt.Features.AddRange(CreateKMLPlacemarks());
      }

      mUsedSymbols = usedSymbols;

      dt.StyleSelectors = new List<geStyleSelector>();

      dt.StyleSelectors.AddRange(CreateKMLStyles());

      st = new geStyle(mStyleName);

      random = new Random();
      color = Color.FromArgb(255, (byte)random.Next(0, 255), (byte)random.Next(0, 255), (byte)random.Next(0, 255));

      st.PolyStyle = new gePolyStyle();

      st.PolyStyle.Color.SysColor = color;
      st.PolyStyle.Fill = true;
      st.LineStyle = new geLineStyle();
      st.LineStyle.Color.SysColor = color;


      dt.StyleSelectors.Add(st);

      return dt;
    }

    private IEnumerable<geStyleSelector> CreateKMLStyles()
    {
      List<geStyleSelector> styles;

      styles = new List<geStyleSelector>();

      foreach(Symbol symbol in mUsedSymbols)
      {
        geStyleSelector style;

        if(symbol != null)
        {
          style = ConvertToKMLStyle(symbol);

          if(style != null)
          {
            styles.Add(style);
          }
        }
      }

      return styles;
    }

    private string GetSchemaName(Graphic graphic)
    {
      return graphic.GetHashCode().ToString() + "schemaid";
    }

    private string CreateKMLPlacemarkDescription(Graphic graphic)
    {
      StringBuilder strBuilder;

      strBuilder = new StringBuilder();

      strBuilder.AppendLine("<table border=\"1\" cellspacing=\"0\" cellpadding=\"1\">");

      foreach(KeyValuePair<string, object> dc in graphic.Attributes)
      {
        if(dc.Key.ToUpper() != "SHAPE" && dc.Key != "FID")
        {
          strBuilder.AppendFormat("<tr><td width=\"100\">{0}</td><td width=\"150\">{1}</td></tr>", dc.Key, graphic.Attributes[dc.Key].ToString());
        }
      }

      strBuilder.AppendLine("</table>");

      return strBuilder.ToString();
    }


    private string CreateKMLSnippet(Graphic graphic)
    {
      string name;

      name = GetPlacemarkName(graphic);


      if(!string.IsNullOrEmpty(name))
      {
        return graphic.Attributes[name].ToString();
      }

      return string.Empty;
    }

    private geSchema CreateKMLPlacemarkSchema(Graphic graphic)
    {
      geSchema schema;
      int pos;

      schema = new geSchema(GetSchemaName(graphic));
      schema.ID = mSchemaName;
      schema.SchemaFields = new List<geSchemaField>();

      foreach(KeyValuePair<string, object> attributeKVP in graphic.Attributes)
      {
        geSimpleField simpleField;

        if(attributeKVP.Key.ToUpper() != "SHAPE" && attributeKVP.Key != "FID")
        {
          pos = attributeKVP.Key.LastIndexOf(".") + 1;
          simpleField = new geSimpleField();
          simpleField.DisplayName = string.Format("<![CDATA[<b>{0}</b>]]>", attributeKVP.Key.Substring(pos, attributeKVP.Key.Length - pos));
          simpleField.FieldType = geSchemaField.FieldTypes.geString;
          simpleField.Name = attributeKVP.Key;

          schema.SchemaFields.Add(simpleField);
        }
      }

      return schema;
    }

    private List<geFeature> CreateKMLPlacemarks()
    {
      List<geFeature> placeMarks;

      placeMarks = new List<geFeature>();

      for(int i = 0; i < mObjects.Count; i++)
      {
        placeMarks.Add(CreateKMLPlacemark(mObjects[i]));
      }

      return placeMarks;
    }

    private gePlacemark CreateKMLPlacemark(Graphic graphic)
    {
      gePlacemark placemark;
      geSchemaData schemaData;
      string placemarkNameColumn;

      placemark = new gePlacemark();

      placemarkNameColumn = GetPlacemarkName(graphic);

      if(placemarkNameColumn != string.Empty)
      {
        placemark.Name = graphic.Attributes[placemarkNameColumn].ToString();
      }

      if(graphic.Symbol != null)
      {
        placemark.StyleUrl = "#" + graphic.Symbol.GetHashCode().ToString();
      }
      else
      {
        placemark.StyleUrl = "#" + mStyleName;
      }

      placemark.Snippet = CreateKMLSnippet(graphic);
      placemark.Description = CreateKMLPlacemarkDescription(graphic);

      placemark.Geometry = ToKMLGeometry(graphic.Geometry);
      placemark.ExtendedData = new geExtendedData();

      schemaData = new geSchemaData();
      schemaData.SchemaUrl = "#" + mSchemaName;

      foreach(KeyValuePair<string, object> attributeKVP in graphic.Attributes)
      {
        if(attributeKVP.Key.ToUpper() != "SHAPE" && attributeKVP.Key != "FID")
        {
          geSimpleData simpleData;

          simpleData = new geSimpleData();
          simpleData.Name = attributeKVP.Key;
          if(graphic.Attributes[attributeKVP.Key] == null)
          {
            simpleData.Value = string.Empty;
          }
          else
          {
            simpleData.Value = graphic.Attributes[attributeKVP.Key].ToString();
          }
          schemaData.SimpleData.Add(simpleData);
        }
      }


      return placemark;

    }

    private string GetPlacemarkName(Graphic graphic)
    {
      string key;

      key = string.Empty;

      foreach(KeyValuePair<string, object> attributeKVP in graphic.Attributes)
      {
        if(key == string.Empty)
        {
          key = attributeKVP.Key;
        }

        if(attributeKVP.Key.ToLower().Contains("name") || attributeKVP.Key.ToLower().Contains("title"))
        {
          return attributeKVP.Key;
        }
      }

      return key;
    }

    private static geGeometry ToKMLGeometry(ESRI.ArcGIS.Client.Geometry.Geometry geometry)
    {
      if(geometry is MapPoint)
      {
        return ToKMLPoint(geometry as MapPoint);
      }
      else if (geometry is Polyline)
      {
        return ToKMLPolyline(geometry as Polyline);
      }
      else if (geometry is Polygon)
      {
        return ToKMLPolygon(geometry as Polygon);
      }
      return null;
    }

    private static geGeometry ToKMLPoint(MapPoint geometry)
    {
      gePoint kmlGeometry;

      kmlGeometry = new gePoint(PointToCoordinate(geometry));

      kmlGeometry.Coordinates.Latitude.Value = geometry.Y;
      kmlGeometry.Coordinates.Longitude.Value = geometry.X;

      return kmlGeometry;
    }

    private static geGeometry ToKMLPolyline(Polyline geometry)
    {
      geLineString kmlGeomtry;

      List<geCoordinates> points = new List<geCoordinates>();

      foreach(ESRI.ArcGIS.Client.Geometry.PointCollection pointCollection in geometry.Paths)
      {
        foreach (MapPoint point in pointCollection)
        {
          points.Add(PointToCoordinate(point));
        }
      }
      kmlGeomtry = new geLineString(points);
      return kmlGeomtry;
    }

    private static geGeometry ToKMLPolygon(Polygon geometry)
    {
      geLineString kmlGeomtry;

      List<geCoordinates> points = new List<geCoordinates>();

      foreach (ESRI.ArcGIS.Client.Geometry.PointCollection ring in geometry.Rings)
      {
        foreach (MapPoint point in ring)
        {
          points.Add(PointToCoordinate(point));
        }
      }
      kmlGeomtry = new geLineString(points);
      return kmlGeomtry;
    }

    private static geCoordinates PointToCoordinate(MapPoint point)
    {
      return new geCoordinates(new geAngle90(point.Y), new geAngle180(point.X));
    }


    public static geStyleSelector ConvertToKMLStyle(Symbol symbol)
    {
      geStyle style;

      style = null;

      if(symbol is MarkerSymbol)
      {
        style = ConvertToPointStyle((MarkerSymbol)symbol);
      }
      else if (symbol is LineSymbol)
      {
        style = ConvertToLineStyle((LineSymbol)symbol);
      }
      else if (symbol is FillSymbol)
      {
        style = ConvertToPolyStyle((FillSymbol)symbol);
      }
      return style;
    }

    private static geStyle ConvertToPointStyle(MarkerSymbol pointSym)
    {
      geIconStyle iconStyle;
      geStyle style;
      string hashId;
      string href;

      hashId = pointSym.GetHashCode().ToString();
      href = string.Format("files/{0}.png", hashId);

      style = new geStyle(hashId);

      iconStyle = new geIconStyle();
      iconStyle.Icon = new geIcon(href);
      iconStyle.Scale = 0.8F;
      iconStyle.HotSpot = new geVec2();
      iconStyle.HotSpot.xunits = geUnitsEnum.insetPixels;
      iconStyle.HotSpot.yunits = geUnitsEnum.insetPixels;
      iconStyle.HotSpot.x = pointSym.OffsetX;
      iconStyle.HotSpot.y = pointSym.OffsetY;

      style.IconStyle = iconStyle;

      return style;
    }

    private static geStyle ConvertToLineStyle(LineSymbol lineSym)
    {
      geLineStyle lineStyle;
      geStyle style;
      string hashId;

      hashId = lineSym.GetHashCode().ToString();
      style = new geStyle(hashId);

      lineStyle = new geLineStyle();
      geColor color = new geColor();
      color.SysColor = ((SolidColorBrush)lineSym.Color).Color;
      lineStyle.Color = color;
      lineStyle.Width = (float)lineSym.Width;

      style.LineStyle = lineStyle;

      return style;
    }

    private static geStyle ConvertToPolyStyle(FillSymbol fillSym)
    {
      gePolyStyle polyStyle;
      geLineStyle border;
      geStyle style;
      string hashId;

      hashId = fillSym.GetHashCode().ToString();
      style = new geStyle(hashId);

      polyStyle = new gePolyStyle();
      geColor color = new geColor();
      color.SysColor = ((SolidColorBrush)fillSym.Fill).Color;
      polyStyle.Color = color;

      border = new geLineStyle();
      geColor borderColor = new geColor();
      borderColor.SysColor = ((SolidColorBrush)fillSym.BorderBrush).Color;
      border.Color = borderColor;

      style.LineStyle = border;
      style.PolyStyle = polyStyle;
      return style;
    }
  }
}
