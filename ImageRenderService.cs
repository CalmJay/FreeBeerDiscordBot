using System.Collections.Generic;
using System.Drawing;

namespace DiscordBot
{
    internal class ImageRenderService
    {

        public static Bitmap Combine(string[] files)
        {
            //read all images into memory
            List<System.Drawing.Bitmap> images = new List<System.Drawing.Bitmap>();
            System.Drawing.Bitmap finalImage = null;

            int width = 0;
            int height = 0;

            foreach (string image in files)
            {
                //create a Bitmap from the file and add it to the list
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image);

                //update the size of the final bitmap
                width += bitmap.Width;
                height = bitmap.Height > height ? bitmap.Height : height;

                images.Add(bitmap);
            }

            //create a bitmap to hold the combined image
            finalImage = new System.Drawing.Bitmap(width, height);
            return finalImage;
        }
    }



}
