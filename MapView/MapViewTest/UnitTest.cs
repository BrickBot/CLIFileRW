#region Using directives

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using MappedView;

#endregion

namespace MapViewTest {
  [TestFixture]
  public class MapViewUnitTest {
    [Test]
    public void ReadTest() {
      string fileName = System.Environment.ExpandEnvironmentVariables("%TEMP%\\MapViewUnitTest.txt");
      StreamWriter f = File.CreateText(fileName);
      for (int i = 0; i < 1000; i++)
        f.Write(string.Format("+{0:d3}", i));
      f.Close();
      MappedFile map = new MappedFile(fileName);
      Assert.AreEqual(map.Length, 4000);
      Random r = new Random(DateTime.Now.Millisecond);
      for (int i = 0; i < 100; i++) {
        int test = r.Next(0, 999);
        MapPtr p = map.Start + (test * 4);
        Assert.IsTrue(p.Matches(string.Format("+{0:d3}", test)));
      }
      map.Dispose();
      File.Delete(fileName);
    }
  }
}
