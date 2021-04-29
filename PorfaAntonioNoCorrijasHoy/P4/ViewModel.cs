using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace P4
{
    public class VMDron : Dron
    {
        public Image Img;
        public ContentControl CCImg;
        public int Zoom;
        public int Angulo;
        public CompositeTransform Transform;

        public VMDron(Dron dron)
            {
            Id = dron.Id;
            Nombre = dron.Nombre;
            Imagen = dron.Imagen;
            Explicacion = dron.Explicacion;
            Estado = dron.Estado;
            X = dron.X;
            Y = dron.Y;
            RX = dron.RX;
            RY = dron.RY;
            Img = new Image();
            string s = System.IO.Directory.GetCurrentDirectory() + "\\" + dron.Imagen;
            Img.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(s));
            Img.Width = 50;
            Img.Height = 50;

            //Lo comento aquí porque lo creo en el Image ItemClick y da conflicto si asigno los dos a la misma imagen
            //CCImg = new ContentControl();
            //CCImg.Content = Img;
            //CCImg.UseSystemFocusVisuals = true;

            Angulo = 0;
            Transform = new CompositeTransform();
            Transform.Rotation = Angulo;

            //No hacer esto
            //Transform.TranslateX = X - 25;
            //Transform.TranslateY = Y - 25;
            Transform.CenterX = 25;
            Transform.CenterY = 25;


            //CCImg.Visibility = Windows.UI.Xaml.Visibility.Visible;//.Collapsed;
            //Mapa.Children.Add(CCImg);
            //Mapa.Children.Last().SetValue(Canvas.LeftProperty, X - 25);
            //Mapa.Children.Last().SetValue(Canvas.TopProperty, Y - 25);
        }
    }


   
}
