using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Raylib_cs;

namespace HelloWorld {
  public class HtmlEl {
    public HtmlEl (TagTypes tag) {
      this.Tag = tag;
    }
    public TagTypes Tag { get; }
    public string Content { get; set; }
    public int Y { get; set; }
    public int FontSize {
      get {
        switch (this.Tag) {

          case TagTypes.h1:
            return 36;
          case TagTypes.h2:
            return 35;
          case TagTypes.h3:
            return 34;
          case TagTypes.h4:
            return 33;
          case TagTypes.h5:
            return 32;
          case TagTypes.h6:
            return 32;
          default:
            return 32;
        }
      }
    }
  }
  public enum TagTypes {
    p,
    h1,
    h2,
    h3,
    h4,
    h5,
    h6,
    // ul,
    // ol,
    // li,
    // a
  }
  static class Program {
    private static bool DarkMode => false;
    private static bool DebugView => false;
    private static float ScrollSpeed => 50.0f;

    public static async Task Main () {
      bool searchBarIsFocused = false;
      Rectangle windowRect = new Rectangle (0, 0, 1600, 800);
      int searchBarBottom = 40;
      string url = "https://dailymail.co.uk";
      string searchBarText = url;
      var searchBarRect = new Rectangle (0, 0, windowRect.width, searchBarBottom);

      float viewportY = 0;
      using var client = new HttpClient ();
      var content = await client.GetStringAsync (url);
      var doc = GetDoc (content).ToArray ();
      // var agilityDoc = new HtmlDocument();
      // agilityDoc.LoadHtml(content);

      Raylib.SetConfigFlags (ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_WINDOW_UNFOCUSED);
      Raylib.InitWindow ((int) windowRect.width, (int) windowRect.height, "Browser");
      Raylib.SetTargetFPS (60);
      Raylib.SetWindowMonitor (1);
      var lightFont = Raylib.LoadFont ("OpenSans-Light.ttf");
      var regularFont = Raylib.LoadFont ("OpenSans-Regular.ttf");
      var boldFont = Raylib.LoadFont ("OpenSans-Bold.ttf");

      while (!Raylib.WindowShouldClose ()) {
        if (Raylib.IsWindowResized ()) {
          windowRect.width = Raylib.GetScreenWidth ();
          windowRect.height = Raylib.GetScreenHeight ();
        }
        if (Raylib.IsMouseButtonDown (0)) {
          searchBarIsFocused = Raylib.CheckCollisionPointRec (Raylib.GetMousePosition (), searchBarRect);
        }

        viewportY += (Raylib.GetMouseWheelMove () * ScrollSpeed) * -1;
        if (viewportY < 0) {
          viewportY = 0;
        }
        Raylib.BeginDrawing ();

        Raylib.ClearBackground (DarkMode ? Color.BLACK : Color.WHITE);

        int kp = Raylib.GetKeyPressed ();
        int key = Raylib.GetCharPressed ();

        while (key > 0) {
          var newKey = (char) key;
          if (searchBarIsFocused) {
            searchBarText += newKey.ToString ();
          }
          key = Raylib.GetCharPressed ();
        }
        if (Raylib.IsKeyPressed (KeyboardKey.KEY_ENTER)) {
          content = await client.GetStringAsync (searchBarText);
          doc = GetDoc (content).ToArray ();
        }
        if (Raylib.IsKeyPressed (KeyboardKey.KEY_BACKSPACE)) {
          if (searchBarText.Length > 0) {
            searchBarText = searchBarText.Remove (searchBarText.Length - 1);
          }
        }


        Raylib.DrawRectangleRec (searchBarRect, Color.GRAY);
        Raylib.DrawText (searchBarText, (int) searchBarRect.x + 5, (int) searchBarRect.y, 32, Color.WHITE);
        int renderedRects = 0;
        float y = 12 + searchBarBottom;
        for (int i = 0; i < doc.Length; i++) {

          var vec = new System.Numerics.Vector2 (12, doc[i].Y);
          Font font;
          if (doc[i].Tag == TagTypes.p) {
            font = lightFont;
          } else if (doc[i].Tag == TagTypes.h1 || doc[i].Tag == TagTypes.h2) {
            font = boldFont;
          } else {
            font = regularFont;
          }
          System.Numerics.Vector2 textVec2 = Raylib.MeasureTextEx (font, doc[i].Content, doc[i].FontSize, 0);
          int xPadding = 22;

          float linesNeededFloat = textVec2.X / windowRect.width;
          int linesNeeded = Convert.ToInt32 (linesNeededFloat) + 1;
          float textRecHeight = (textVec2.Y * linesNeeded) + 20;
          var textRec = new Rectangle (xPadding, y - viewportY, windowRect.width - (xPadding * 2), textRecHeight);
          bool shouldDrawRect = Raylib.CheckCollisionRecs (windowRect, textRec);
          if (shouldDrawRect) {
            renderedRects += 1;
            var htmlDoc = new HtmlDocument ();
            htmlDoc.LoadHtml (doc[i].Content);

            Raylib.DrawTextRec (font, $"{doc[i].Content}", textRec, doc[i].FontSize, 0, true, DarkMode ? Color.WHITE : Color.BLACK);
            if (DebugView) {
              Raylib.DrawRectangleLinesEx (textRec, 1, Color.RED);
            }
          }
          y += textRecHeight;
        }
        if (DebugView) {
          Raylib.DrawText ($"Width: {windowRect.width}\nViewport y: {viewportY}\nRendered rects: {renderedRects}\nSearch text: {searchBarText}", Convert.ToInt32 (windowRect.width * 0.85f), 20, 20, DarkMode ? Color.WHITE : Color.BLACK);
        }

        Raylib.EndDrawing ();
      }
      Raylib.CloseWindow ();
    }

    public static List<HtmlEl> GetDoc (string content) {
      int y = 0;
      var tagContentBuilder = new StringBuilder ();
      var result = new List<string> ();
      var htmlElsList = new List<HtmlEl> ();
      var tagTypes = Enum.GetNames (typeof (TagTypes)).ToList ();
      bool openingTagFound = false;
      TagTypes currentParentTag = TagTypes.p;
      HtmlEl htmlEl = null;
      for (int i = 0; i < content.Length; i++) {
        if (!openingTagFound) {
          if (content[i] == '<') {
            i++;
            var openingTagSb = new StringBuilder ();
            while (!char.IsWhiteSpace (content[i]) && content[i] != '>') {
              openingTagSb.Append (content[i]);
              i++;
            }
            while (content[i] != '>') {
              i++;
            }

            if (Enum.TryParse (openingTagSb.ToString (), out currentParentTag)) {
              openingTagFound = true;
              htmlEl = new HtmlEl (currentParentTag);
            }
            continue;
          }
        }

        // collect something actually interesting!
        if (openingTagFound) {
          if (content[i] == '<' && content[i + 1] == '/') {
            i += 2;
            var closingTagSb = new StringBuilder ();
            while (!char.IsWhiteSpace (content[i]) && content[i] != '>') {
              closingTagSb.Append (content[i]);
              i++;
            }
            TagTypes closingTag;
            if (Enum.TryParse (closingTagSb.ToString (), out closingTag)) {
              if (closingTag == currentParentTag) {
                string line = tagContentBuilder.ToString ().Trim ().Replace ('\n', '\0');

                var replacedLine = Regex.Replace (line, "<.*?>", string.Empty);
                if (htmlEl != null && replacedLine.Length > 0) {
                  htmlEl.Content = replacedLine;
                  int elsInList = htmlElsList.Count ();
                  if (elsInList > 0) {
                    y += htmlElsList[elsInList - 1].FontSize + 4;
                  }
                  htmlEl.Y = y;
                  htmlElsList.Add (htmlEl);
                }
                result.Add (replacedLine);
                tagContentBuilder.Clear ();
                openingTagFound = false;
              }
            }

          } else {
            tagContentBuilder.Append (content[i]);
          }
        }
      }
      return htmlElsList;
    }

  }
}