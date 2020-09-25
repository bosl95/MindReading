using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MindReading
{
    [Serializable]
    class DrawData
    {
		public int drawMode = 1;
        public Point startPoint;
        public Point endPoint;
        [NonSerialized]
        public Pen pen;
		public Pen eraser;

		public DrawData(Point x, Point y, Pen mypen,int drawmode)
		{
			startPoint = x;
			endPoint = y;
			pen = new Pen(mypen.Color, mypen.Width);
			eraser = new Pen(Color.White, 10);
            drawMode = drawmode;
            

		}
        public void drawData(Graphics g)
		{

            switch (drawMode)
			{

				case 1:
					g.DrawLine(pen, startPoint, endPoint);
					break;
				case 2:
					g.DrawLine(eraser, startPoint, endPoint);
					break;
			}
			//g.DrawLine(pen, startPoint, endPoint);
		}
        
    }
}
