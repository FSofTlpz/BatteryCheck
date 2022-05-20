#if TOUCHEVALUATOR_SKIA

// getestet mit
//    SkiaSharp                  2.80.3
//    SkiaSharp.Views.Forms      2.80.3

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace FSofTUtils.Xamarin.Touch {
   public class TouchEvaluator {

      Dictionary<long, List<Point>> points;
      Dictionary<long, SKMatrix> matrix;

      public TouchEvaluator() {
         points = new Dictionary<long, List<Point>>();
         matrix = new Dictionary<long, SKMatrix>();
      }

      /// <summary>
      /// wenn noch nicht vorhanden, wird sie erzeugt
      /// </summary>
      /// <param name="fingerid"></param>
      void createList4Id(long fingerid) {
         if (!points.ContainsKey(fingerid))
            points.Add(fingerid, new List<Point>());
         if (!matrix.ContainsKey(fingerid))
            matrix.Add(fingerid, SKMatrix.CreateIdentity());
      }

      /// <summary>
      /// alle registrierten Daten löschen
      /// </summary>
      /// <param name="fingerid"></param>
      public void Clear(long fingerid) {
         createList4Id(fingerid);
         points[fingerid].Clear();
         matrix[fingerid] = SKMatrix.CreateIdentity();
      }

      public void RegisterPressed(long fingerid, Point pt) {
         Clear(fingerid);
         points[fingerid].Add(pt);
      }

      public void RegisterMoved(long fingerid, Point pt) {
         createList4Id(fingerid);
         points[fingerid].Add(pt);
      }

      public void RegisterReleased(long fingerid, Point pt) {
         points[fingerid].Add(pt);
      }



      /// <summary>
      /// liefert alle registrierten Punkte
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public List<Point> GetPoints(long fingerid) {
         createList4Id(fingerid);
         return points[fingerid];
      }

      /// <summary>
      /// Anzahl der Finger für die Punkte registriert sind
      /// </summary>
      /// <returns></returns>
      public int Fingers() {
         int fingers = 0;
         foreach (var item in points) {
            if (item.Value.Count > 0)
               fingers++;
         }
         return fingers;
      }

      /// <summary>
      /// liefert die ID für Finger 1, 2 usw.
      /// </summary>
      /// <param name="no">1, ...</param>
      /// <returns></returns>
      public long GetValidFingerId(int no) {
         long id = -1;
         int count = 0;
         foreach (var item in points) {
            if (item.Value.Count > 0) {
               count++;
               if (count == no) {
                  id = item.Key;
                  break;
               }
            }
         }
         return id;
      }


      /// <summary>
      /// liefert die 1. Finger-ID die ungleich der gelieferten ist
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public long PivotFinger(long fingerid) {
         foreach (var item in points.Keys) {
            if (item != fingerid)
               return item;
         }
         return -1;
      }

      /// <summary>
      /// registrierte Punktanzahl für diese ID
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      int count4ID(long fingerid) {
         return points[fingerid].Count;
      }

      /// <summary>
      /// liefert den n-ten Punkt für diese ID
      /// </summary>
      /// <param name="fingerid"></param>
      /// <param name="n"></param>
      /// <returns></returns>
      Point getN(long fingerid, int n) {
         createList4Id(fingerid);
         if (count4ID(fingerid) <= n)
            return new Point(-1, -1);
         return points[fingerid][n];
      }

      /// <summary>
      /// liefert den 1. Punkt
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public Point GetFirstPoint(long fingerid) {
         return getN(fingerid, 0);
      }

      /// <summary>
      /// liefert den letzten Punkt
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public Point GetLastPoint(long fingerid) {
         return getN(fingerid, count4ID(fingerid) - 1);
      }

      /// <summary>
      /// liefert den vorletzten Punkt
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public Point GetSecondLastPoint(long fingerid) {
         return getN(fingerid, count4ID(fingerid) - 2);
      }


      /// <summary>
      /// liefert die Bewegung des letzten <see cref="RegisterMoved(long, Point)"/> als Verschiebung
      /// </summary>
      /// <param name="view"></param>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public SKMatrix GetTranslateMatrix(SKCanvasView view, long fingerid) {
         SKPoint ptPrev = Convert(view, GetSecondLastPoint(fingerid));
         SKPoint ptAct = Convert(view, GetLastPoint(fingerid));
         return SKMatrix.CreateTranslation(ptAct.X - ptPrev.X, ptAct.Y - ptPrev.Y); ;
      }

      /// <summary>
      /// liefert die Bewegung des letzten <see cref="RegisterMoved(long, Point)"/> als Zoom
      /// </summary>
      /// <param name="view"></param>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public SKMatrix GetPinchMatrix(SKCanvasView view, long fingerid) {
         long pivotid = PivotFinger(fingerid);   // ID des "festen" Fingers

         // Get the three points involved in the transform
         SKPoint ptPivot = Convert(view, GetLastPoint(pivotid));
         SKPoint ptPrev = Convert(view, GetSecondLastPoint(fingerid));
         SKPoint ptAct = Convert(view, GetLastPoint(fingerid));

         // Calculate two vectors
         SKPoint oldVector = ptPrev - ptPivot;
         SKPoint newVector = ptAct - ptPivot;

         // Scaling factors are ratios of those
         float scaleX = newVector.X / oldVector.X;
         float scaleY = newVector.Y / oldVector.Y;

         if (!float.IsNaN(scaleX) && !float.IsInfinity(scaleX) &&
             !float.IsNaN(scaleY) && !float.IsInfinity(scaleY))
            // If something bad hasn't happened, calculate a scale and translation matrix
            return SKMatrix.CreateScale(scaleX, scaleY, ptPivot.X, ptPivot.Y);

         return new SKMatrix();
      }

      //public SKMatrix GetPinchMatrix(SKCanvasView view) {
      //   long id1 = getValidFingerId(0);
      //   long id2 = getValidFingerId(1);
      //   if (id1 >= 0 && id2 >= 0) {
      //      SKPoint oldVector = Convert(view, GetFirstPoint(id1)) - Convert(view, GetLastPoint(id1));
      //      SKPoint newVector = Convert(view, GetFirstPoint(id2)) - Convert(view, GetLastPoint(id2));

      //      float scaleX = newVector.X / oldVector.X;
      //      float scaleY = newVector.Y / oldVector.Y;

      //      if (!float.IsNaN(scaleX) && !float.IsInfinity(scaleX) &&
      //          !float.IsNaN(scaleY) && !float.IsInfinity(scaleY))
      //         return SKMatrix.CreateScale(scaleX, scaleY);
      //   }
      //   return new SKMatrix();
      //}

      public double GetZoom(SKCanvasView view) {
         double scale = 1;
         long id1 = GetValidFingerId(1);
         long id2 = GetValidFingerId(2);
         if (id1 >= 0 && id2 >= 0) {

            //if (count4ID(id1) > 5)
            //   Debug.WriteLine("");

            SKPoint startVector = Convert(view, GetFirstPoint(id1)) - Convert(view, GetFirstPoint(id2));
            float len = startVector.Length;
            Debug.WriteLine("---");
            Debug.WriteLine("GetZoom: startVector {0}, len={1}", startVector, len);
            if (len > 0) {
               SKPoint actualVector = Convert(view, GetLastPoint(id1)) - Convert(view, GetLastPoint(id2));
               scale = actualVector.Length / len;
               Debug.WriteLine("GetZoom: actualVector {0}, len={1}", actualVector, actualVector.Length);
            }
         }
         Debug.WriteLine("GetZoom: scale={0}", scale);
         return scale;
      }



      /// <summary>
      /// liefert die Bewegung des letzten <see cref="RegisterMoved(long, Point)"/> als Rotation
      /// </summary>
      /// <param name="view"></param>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public SKMatrix GetRotateMatrix(SKCanvasView view, long fingerid) {
         long pivotid = PivotFinger(fingerid);   // ID des "festen" Fingers

         // Get the three points involved in the transform
         SKPoint ptPivot = Convert(view, GetLastPoint(pivotid));
         SKPoint ptPrev = Convert(view, GetSecondLastPoint(fingerid));
         SKPoint ptAct = Convert(view, GetLastPoint(fingerid));

         // Calculate two vectors
         SKPoint oldVector = ptPrev - ptPivot;
         SKPoint newVector = ptAct - ptPivot;

         // Find angles from pivot point to touch points
         float oldAngle = (float)Math.Atan2(oldVector.Y, oldVector.X);
         float newAngle = (float)Math.Atan2(newVector.Y, newVector.X);

         // Calculate rotation matrix
         float angle = newAngle - oldAngle;
         SKMatrix touchMatrix = SKMatrix.CreateRotation(angle, ptPivot.X, ptPivot.Y);

         // Effectively rotate the old vector
         float magnitudeRatio = oldVector.Length / newVector.Length;
         oldVector.X = magnitudeRatio * newVector.X;
         oldVector.Y = magnitudeRatio * newVector.Y;

         // Isotropic scaling!
         float scale = newVector.Length / oldVector.Length;

         if (!float.IsNaN(scale) && !float.IsInfinity(scale))
            touchMatrix.PostConcat(SKMatrix.CreateScale(scale, scale, ptPivot.X, ptPivot.Y));


         return touchMatrix;
      }

      /// <summary>
      /// Bewegungsschritt anfügen
      /// </summary>
      /// <param name="newmove"></param>
      /// <param name="fingerid"></param>
      public void AddMoveMatrix(SKMatrix newmove, long fingerid) {
         matrix[fingerid] = matrix[fingerid].PostConcat(newmove);
      }

      /// <summary>
      /// liefert die summierte Matrix, die seit dem letzten <see cref="RegisterPressed(long, Point)"/> mit <see cref="AddMoveMatrix(SKMatrix, long)"/> aufgebaut wurde
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public SKMatrix GetSumMatrix(long fingerid) {
         return matrix[fingerid];
      }

      /// <summary>
      /// liefert die Verschiebung aus der summierten Matrix, die seit dem letzten <see cref="RegisterPressed(long, Point)"/> mit <see cref="AddMoveMatrix(SKMatrix, long)"/> aufgebaut wurde
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public Size GetSumMatrixTranslation(long fingerid) {
         return new Size(matrix[fingerid].TransX, matrix[fingerid].TransY);
      }

      /// <summary>
      /// liefert die Skalierung aus der summierten Matrix, die seit dem letzten <see cref="RegisterPressed(long, Point)"/> mit <see cref="AddMoveMatrix(SKMatrix, long)"/> aufgebaut wurde
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public Size GetSumMatrixScale(long fingerid) {
         return new Size(matrix[fingerid].ScaleX, matrix[fingerid].ScaleY);
      }

      /// <summary>
      /// liefert die Drehung (cos(alpha)) aus der summierten Matrix, die seit dem letzten <see cref="RegisterPressed(long, Point)"/> mit <see cref="AddMoveMatrix(SKMatrix, long)"/> aufgebaut wurde
      /// </summary>
      /// <param name="fingerid"></param>
      /// <returns></returns>
      public double GetSumMatrixRotation(long fingerid) {
         return matrix[fingerid].SkewY;
      }


      public static SKPoint Convert(SKCanvasView view, Point pt) {
         return new SKPoint((float)(view.CanvasSize.Width * pt.X / view.Width),
                            (float)(view.CanvasSize.Height * pt.Y / view.Height));
      }

   }
}
#endif