using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace FSofTUtils.Xamarin.Touch {
   public class TouchPointSampler {

      class PointData {

         public readonly Point Point;
         public readonly DateTime DateTime;

         public PointData(Point pt) {
            Point = pt;
            DateTime = DateTime.Now;
         }

      }

      Dictionary<long, List<PointData>> pointlsts;


      /// <summary>
      /// Anzahl der Punktlisten
      /// </summary>
      public int IdCount {
         get {
            return pointlsts.Count;
         }
      }


      public TouchPointSampler() {
         pointlsts = new Dictionary<long, List<PointData>>();
      }

      /// <summary>
      /// Punktliste leeren
      /// </summary>
      /// <param name="id"></param>
      public void Clear(long id) {
         if (pointlsts.ContainsKey(id))
            pointlsts[id].Clear();
      }

      /// <summary>
      /// alle Punktlisten leeren
      /// </summary>
      public void Clear() {
         foreach (var item in pointlsts)
            Clear(item.Key);
         pointlsts.Clear();
      }

      /// <summary>
      /// neuen Punkt registrieren
      /// </summary>
      /// <param name="id"></param>
      /// <param name="pt"></param>
      public void Add(long id, Point pt) {
         if (!pointlsts.ContainsKey(id))
            pointlsts.Add(id, new List<PointData>());
         pointlsts[id].Add(new PointData(pt));
      }

      /// <summary>
      /// Delta zum 1. Punkt
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      public Point GetFullDelta(long id) {
         if (pointlsts.ContainsKey(id) &&
             pointlsts[id].Count > 1)
            return new Point(pointlsts[id][pointlsts[id].Count - 1].Point.X - pointlsts[id][0].Point.X,
                             pointlsts[id][pointlsts[id].Count - 1].Point.Y - pointlsts[id][0].Point.Y);
         return Point.Zero;
      }

      /// <summary>
      /// Zeitdifferenz zum 1. Punkt in Millisekunden
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      public double GetFullDeltaTime(long id) {
         if (pointlsts.ContainsKey(id) &&
             pointlsts[id].Count > 1)
            return pointlsts[id][pointlsts[id].Count - 1].DateTime.Subtract(pointlsts[id][0].DateTime).TotalMilliseconds;
         return 0;
      }

      /// <summary>
      /// Delta zum vorher registrierten Punkt
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      public Point GetLastDelta(long id) {
         if (pointlsts.ContainsKey(id) &&
             pointlsts[id].Count > 1)
            return new Point(pointlsts[id][pointlsts[id].Count - 1].Point.X - pointlsts[id][pointlsts[id].Count - 2].Point.X,
                             pointlsts[id][pointlsts[id].Count - 1].Point.Y - pointlsts[id][pointlsts[id].Count - 2].Point.Y);
         return Point.Zero;
      }

      /// <summary>
      /// liefert den 1. Punkt zur ID
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      public Point GetStartPoint(long id) {
         if (pointlsts.ContainsKey(id) &&
             pointlsts[id].Count > 1)
            return pointlsts[id][0].Point;
         return Point.Zero;
      }

      /// <summary>
      /// liefert den letzten Punkt zur ID
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      public Point GetLastPoint(long id) {
         if (pointlsts.ContainsKey(id) &&
             pointlsts[id].Count > 1)
            return pointlsts[id][Count(id) - 1].Point;
         return Point.Zero;
      }

      /// <summary>
      /// Anzahl der Punkte in dieser Liste
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      public int Count(long id) {
         return pointlsts[id].Count;
      }

      /// <summary>
      /// liefert die akt. registrierten ID's ("eine je Finger")
      /// </summary>
      /// <returns></returns>
      public long[] UsedIds() {
         long[] ids = new long[pointlsts.Count];
         pointlsts.Keys.CopyTo(ids, 0);
         return ids;
      }

      double startPointDistance(long[] usedids) {
         if (usedids.Length > 1)
            return pointlsts[usedids[0]][0].Point.Distance(pointlsts[usedids[1]][0].Point);
         return 0;
      }

      double lastPointDistance(long[] usedids) {
         if (usedids.Length > 1)
            return pointlsts[usedids[0]][pointlsts[usedids[0]].Count - 1].Point.Distance(pointlsts[usedids[1]][pointlsts[usedids[1]].Count - 1].Point);
         return 0;
      }

      public double GetDistanceRelation() {
         long[] usedids = UsedIds();
         if (usedids.Length > 1) {
            double dist1 = startPointDistance(usedids);
            if (dist1 > 0)
               return dist1 > 0 ? lastPointDistance(usedids) / dist1 : 1;
         }
         return 1;
      }

      public override string ToString() {
         StringBuilder sb = new StringBuilder();

         long[] ids = UsedIds();
         foreach (var item in ids) {
            sb.AppendFormat("[ID {0}, {1} points] ", item, pointlsts[item].Count);
         }

         return sb.ToString();
      }

   }
}
