using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using devDept.Eyeshot;
using devDept.Graphics;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using devDept.Eyeshot.Labels;
using Environment = devDept.Eyeshot.Environment;
using Point = System.Drawing.Point;
using devDept.Controls.WinForms;
using devDept.Eyeshot.Translators;
using devDept.Serialization;

namespace WindowsApplication1
{
    public partial class Form1 : Form
    {
        private Camera secondCamera;

        private MyHiddenLinesViewPrint hdlView1, hdlView2;

        private bool _yAxisUp = false;

        // Pens used to draw the lines
        private Pen PenEdge, PenSilho, PenWire;

        public Form1()
        {
            InitializeComponent();

            // model1.Unlock(""); // For more details see 'Product Activation' topic in the documentation.
            model1.WorkCompleted += model2_WorkCompleted;


        }

        protected override void OnLoad(EventArgs e)
        {

            // Creates the pens
            PenSilho = new Pen(Color.Black, 3.0f);
            PenEdge = new Pen(Color.Black, 1.0f);
            PenWire = new Pen(Color.Black, 1.0f);
            PenEdge.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            PenSilho.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            PenWire.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);


            model1.Grid.AutoSize = true;
            model1.Grid.Step = 50;
            model1.Camera.FocalLength = 30;
            model1.Camera.ProjectionMode = projectionType.Perspective;            
            model1.SetView(viewType.Trimetric);
            model1.ZoomFit();

            // Imports an Ascii model
            
         /*   devDept.Eyeshot.Translators.ReadFile rf = new devDept.Eyeshot.Translators.ReadFile("../../../../../../dataset/Assets/house.eye");
            rf.DoWork();
            model1.Entities.AddRange(rf.Entities, Color.Gray);
            

            // Changes the color/material of the fifth entity
            rf.Entities[5].Color = Color.Pink;*/
            
            model1.ZoomFit();
            comboBoxPrintMode.SelectedIndex = 0;
        }

        private void printButton_Click(object sender, EventArgs e)
        {
            // Defines the camera for the second view                        
            secondCamera = new Camera(new Point3D(320, 0, 160),
                                           600,
                                           new Quaternion(Vector3D.AxisZ, 90),
                                           projectionType.Orthographic,
                                           50,
                                           1);

            if (comboBoxPrintMode.SelectedIndex == 0) // Vector printing
            {
                hdlView1 = new MyHiddenLinesViewPrint(new HiddenLinesViewSettings(model1.Viewports[0], model1, 0.1, true, PenSilho, PenEdge, PenWire, false));
                model1.StartWork(hdlView1);
            }
            else // Raster printing
            {
                secondCamera.Move(50, 50, 0);

                // Prints the page
                Print();
            }
        }

        private void model1_WorkCompleted(object sender, WorkCompletedEventArgs e)
        {
            if (e.WorkUnit == hdlView1)
            {
                var prevCam = model1.Viewports[0].Camera;

                model1.Viewports[0].Camera = secondCamera;

                hdlView2 = new MyHiddenLinesViewPrint(new HiddenLinesViewSettings(model1.Viewports[0], model1, 0.1, PenSilho, PenEdge, PenWire, false));

                model1.Viewports[0].Camera = prevCam;

                // Runs the hidden lines computation for the second view
                model1.StartWork(hdlView2);
            }
            else if (e.WorkUnit == hdlView2)
            {
                // Prints the page
                Print();
            }

        }

        public void Print()
        {
            PrintPreviewDialog ppDlg = new PrintPreviewDialog();

            ppDlg.Document = printDocument1;

            // Sets the property to true to have the drawing correctly centered on the page
            printDocument1.OriginAtMargins = true;

            try
            {
                ppDlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            RectangleF printable = e.MarginBounds;

            // Since PrintDocument.OriginAtMargins = True, sets top-Left corner to (0,0)
            printable.X = 0;
            printable.Y = 0;

            // Draws the logo
            int logoSize = 70;
            e.Graphics.DrawImage(Properties.Resources.logo, printable.Right - logoSize, printable.Y, logoSize, logoSize);

            //////////////
            // Draws the main title and some text
            //////////////

            string title = "Advanced Printing sample";

            Font titleFont = new Font(Font.FontFamily, 30, FontStyle.Regular);

            SizeF stringSize = e.Graphics.MeasureString(title, titleFont);

            float nextY = printable.Top + logoSize / 2;

            e.Graphics.DrawString(title, titleFont, Brushes.Blue, printable.Left + printable.Width / 2 - stringSize.Width / 2, nextY);

            int verticalOffset = 60;

            nextY += stringSize.Height + 20;

            Font textFont = new Font(Font.FontFamily, 12, FontStyle.Regular);

            string text = "This sample demonstrates how to draw different views of the same model in the proper page area.";
            stringSize = e.Graphics.MeasureString(text, textFont);

            e.Graphics.DrawString(text, textFont, Brushes.Black, new RectangleF(printable.X, nextY, printable.Width, printable.Height));

            nextY += stringSize.Height + verticalOffset;

            // Defines a margin
            int marginFromBorder = 5;

            // Draw the views
            if (comboBoxPrintMode.SelectedIndex == 0) // Vector
                PrintPageVector(sender, e, ref nextY, verticalOffset, printable, marginFromBorder);
            else // Raster
                PrintPageRaster(sender, e, ref nextY, verticalOffset, printable, marginFromBorder);

            //////////////
            // Draws some other text
            //////////////

            Font titleFont2 = new Font(Font.FontFamily, 20, FontStyle.Bold);
            string title2 = "Window opening details";

            SizeF title2Size = e.Graphics.MeasureString(title2, titleFont2);

            e.Graphics.DrawString(title2, titleFont2, Brushes.Blue, printable.Left + printable.Width / 2 - title2Size.Width / 2, nextY + verticalOffset);

            nextY += 2 * (marginFromBorder + verticalOffset);

            text = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

            e.Graphics.DrawString(text, textFont, Brushes.Black, new RectangleF(printable.Left, nextY, printable.Width / 2 - 20, printable.Height - nextY));
        }

        private void pageSetupButton_Click(object sender, EventArgs e)
        {
            PageSetupDialog pageSetupDialog = new PageSetupDialog();

            pageSetupDialog.Document = printDocument1;
            pageSetupDialog.AllowMargins = true;

            if (pageSetupDialog.ShowDialog() == DialogResult.OK)

                printDocument1 = pageSetupDialog.Document;
        }

        private void exportToEMFButton_Click(object sender, EventArgs e)
        {
            // It is possible to save in DWG / DXF with the HiddenLinesViewOnFileAutodesk class available in x86 and x64 dlls
            model1.WriteToFileVector(true, "house.emf");
        }

        private void PrintPageVector(object sender, System.Drawing.Printing.PrintPageEventArgs e,
                                     ref float nextY, int verticalOffset, RectangleF printable, int marginFromBorder)
        {
            //////////////
            // First View
            //////////////

            hdlView1.PrintRect = new RectangleF(printable.Left, nextY, printable.Width - 20, 200);

            // Draws the first view
            DrawViewFrame(e.Graphics, "First View", hdlView1.PrintRect, marginFromBorder);

            hdlView1.Print(e);

            nextY += 200;

            //////////////
            // Second View
            //////////////

            hdlView2.PrintRect = new RectangleF((printable.Left + printable.Width / 2), nextY + 2 * (marginFromBorder + verticalOffset), printable.Width / 2 - 20, 400);

            // Draws the second view
            DrawViewFrame(e.Graphics, "Second View", hdlView2.PrintRect, marginFromBorder);

            hdlView2.Print(e);
        }

        private void PrintPageRaster(object sender, System.Drawing.Printing.PrintPageEventArgs e,
                                     ref float nextY, int verticalOffset, RectangleF printable, int marginFromBorder)
        {
            //////////////
            // First View
            //////////////

            RectangleF printRect1 = new RectangleF(printable.Left, nextY, printable.Width - 20, 200);

            // Draws the first view with a 4x resolution for better quality
            Bitmap bmp1 = model1.RenderToBitmap(new Size((int)printRect1.Width * 4, (int)printRect1.Height * 4));

            Size scaledSize;
            ScaleImageSizeToPrintRect(printRect1, bmp1.Size, out scaledSize);

            DrawViewFrame(e.Graphics, "First View", printRect1, marginFromBorder);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(bmp1, (int)(printRect1.Left + (printRect1.Width - scaledSize.Width) / 2), (int)(printRect1.Top + (printRect1.Height - scaledSize.Height) / 2), scaledSize.Width, scaledSize.Height);

            nextY += 200;

            //////////////
            // Second View
            //////////////

            Camera oldCamera = model1.Viewports[0].Camera;
            model1.Viewports[0].Camera = secondCamera;

            // Draws the second view
            RectangleF printRect2 = new RectangleF((printable.Left + printable.Width / 2), nextY + 2 * (marginFromBorder + verticalOffset), printable.Width / 2 - 20, 400);

            // Set the second view viewport size
            Size oldsize = model1.Size;
            model1.Size = new Size(200, 250);

            // Draws the second view with a 4x resolution for better quality
            Bitmap bmp2 = model1.RenderToBitmap(new Size((int)printRect2.Width * 1, (int)printRect2.Height * 1));

            // restore previous view viewport size
            model1.Size = oldsize;

            ScaleImageSizeToPrintRect(printRect2, bmp2.Size, out scaledSize);

            DrawViewFrame(e.Graphics, "Second View", printRect2, marginFromBorder);

            e.Graphics.DrawImage(bmp2, (int)(printRect2.Left + (printRect2.Width - scaledSize.Width) / 2), (int)(printRect2.Top + (printRect2.Height - scaledSize.Height) / 2), scaledSize.Width, scaledSize.Height);

            // restore previous camera
            model1.Viewports[0].Camera = oldCamera;
            model1.Refresh();
        }

      

        private void importButton_Click(object sender, EventArgs e)
        {
            using (var importFileDialog1 = new OpenFileDialog())
            using (var importFileAddOn = new ImportFileAddOn())
            {
                string theFilter = "All compatible file types (*.*)|*.asc;*.stl;*.obj;*.las;*.3ds"
#if NURBS 
                               + ";*.igs;*.iges;*.stp;*.step"
#endif

#if SOLID
                               + ";*.ifc;*.ifczip"
#endif
                               + "|Points (*.asc)|*.asc|" + "WaveFront OBJ (*.obj)|*.obj|" + "Stereolithography (*.stl)|*.stl|" + "Laser LAS (*.las)|*.las|" + "3D Studio Max (*.3ds)|*.3ds";
#if NURBS
                theFilter += "|IGES (*.igs; *.iges)|*.igs; *.iges|" + "STEP (*.stp; *.step)|*.stp; *.step";
#endif

#if SOLID
                theFilter += "|IFC (*.ifc; *.ifczip)|*.ifc; *.ifczip";
#endif
                importFileDialog1.Filter = theFilter;

                importFileDialog1.Multiselect = false;
                importFileDialog1.AddExtension = true;
                importFileDialog1.CheckFileExists = true;
                importFileDialog1.CheckPathExists = true;

                if (importFileDialog1.ShowDialog(importFileAddOn, this) == DialogResult.OK)
                {
                    model1.Clear();
                    _yAxisUp = importFileAddOn.YAxisUp;

                    ReadFileAsync rfa = getReader(importFileDialog1.FileName);

                    if (rfa != null)
                    {
                        model1.StartWork(rfa);

                        model1.SetView(viewType.Trimetric, true, model1.AnimateCamera);

                        /*openButton.Enabled = false;
                        saveButton.Enabled = false;
                        importButton.Enabled = false;*/
                    }
                }
            }
        }

        private ReadFileAsync getReader(string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName);

            if (ext != null)
            {
                ext = ext.TrimStart('.').ToLower();

                switch (ext)
                {
                    case "asc":
                        return new ReadASC(fileName);
                    case "stl":
                        return new ReadSTL(fileName);
                    case "obj":
                        return new ReadOBJ(fileName);
                    case "las":
                        return new ReadLAS(fileName);
                    case "3ds":
                        return new Read3DS(fileName);
#if NURBS
                    case "igs":
                    case "iges":
                        return new ReadIGES(fileName);
                    case "stp":
                    case "step":
                        return new ReadSTEP(fileName);
#endif
#if SOLID
                    case "ifc":
                    case "ifczip":
                        return new ReadIFC(fileName);
#endif
                }
            }

            return null;
        }

        private OpenFileAddOn _openFileAddOn;
        private void openButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog1 = new OpenFileDialog())
            {
                openFileDialog1.Filter = "Eyeshot (*.eye)|*.eye";
                openFileDialog1.Multiselect = false;
                openFileDialog1.AddExtension = true;
                openFileDialog1.CheckFileExists = true;
                openFileDialog1.CheckPathExists = true;
                openFileDialog1.DereferenceLinks = true;

                _openFileAddOn = new OpenFileAddOn();
                _openFileAddOn.EventFileNameChanged += OpenFileAddOn_EventFileNameChanged;

                if (openFileDialog1.ShowDialog(_openFileAddOn, this) == DialogResult.OK)
                {
                    _yAxisUp = false;
                    model1.Clear();
                    ReadFile readFile = new ReadFile(openFileDialog1.FileName, (contentType)_openFileAddOn.ContentOption);
                    model1.StartWork(readFile);
                    model1.SetView(viewType.Trimetric, true, model1.AnimateCamera);
                  //  openButton.Enabled = false;
                }

                _openFileAddOn.EventFileNameChanged -= OpenFileAddOn_EventFileNameChanged;
                _openFileAddOn.Dispose();
                _openFileAddOn = null;
            }
        }
        private void model2_WorkCompleted(object sender, devDept.Eyeshot.WorkCompletedEventArgs e)
        {
            if(e.WorkUnit is ReadFileAsync)
            {
                ReadFileAsync rfa = (ReadFileAsync)e.WorkUnit;
                rfa.AddToScene(model1);
                model1.SetView(viewType.Trimetric, true, model1.AnimateCamera);
            }
        }

        private void OpenFileAddOn_EventFileNameChanged(IWin32Window sender, string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                ReadFile rf = new ReadFile(filePath, true);
                _openFileAddOn.SetFileInfo(rf.GetThumbnail(), rf.GetFileInfo());
            }
            else
            {
                _openFileAddOn.ResetFileInfo();
            }
        }

        private void eyeSaveButton_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            using (var saveFileAddOn = new SaveFileAddOn())
            {
                saveFileDialog.Filter = "Eyeshot (*.eye)|*.eye";
                saveFileDialog.AddExtension = true;
                saveFileDialog.CheckPathExists = true;

                if (saveFileDialog.ShowDialog(saveFileAddOn, this) == DialogResult.OK)
                {
                    WriteFile writeFile = new WriteFile(new WriteFileParams(model1) { Content = (contentType)saveFileAddOn.ContentOption, SerializationMode = (serializationType)saveFileAddOn.SerialOption, SelectedOnly = saveFileAddOn.SelectedOnly, Purge = saveFileAddOn.Purge }, saveFileDialog.FileName);
                    model1.StartWork(writeFile);
                   /* openButton.Enabled = false;
                    saveButton.Enabled = false;
                    importButton.Enabled = false;*/
                }
            }
        }

        

        private void ScaleImageSizeToPrintRect(RectangleF printRect, Size imageSize, out Size scaledSize)
        {
            double width, height;
            double ratio;

            // fit the width of the image inside the width of the print Rectangle
            ratio = printRect.Width / imageSize.Width;
            width = imageSize.Width * ratio;
            height = imageSize.Height * ratio;

            // fit the other dimension
            if (height > printRect.Height)
            {
                ratio = printRect.Height / height;
                width *= ratio;
                height *= ratio;
            }

            scaledSize = new Size((int)width, (int)height);
        }

        private void DrawViewFrame(Graphics graphics, string title, RectangleF printRect, int marginFromBorder)
        {
            Rectangle borderRectangle = new Rectangle((int)(printRect.X - marginFromBorder),
                                                        (int)(printRect.Y - marginFromBorder),
                                                        (int)(printRect.Width + 2 * marginFromBorder),
                                                        (int)(printRect.Height + 2 * marginFromBorder));

            // Draws the view title
            SizeF titleSize = graphics.MeasureString(title, Font);
            graphics.DrawString(title, Font, Brushes.Black, borderRectangle.Left + borderRectangle.Width / 2 - titleSize.Width / 2, borderRectangle.Top - titleSize.Height);

            // Draws a shadow rectangle
            graphics.FillRectangle(Brushes.LightGray, borderRectangle.Left + 10, borderRectangle.Top + 10, borderRectangle.Width, borderRectangle.Height);
            graphics.FillRectangle(Brushes.White, borderRectangle);

            // Draws the border of the viewport
            Pen borderPen = new Pen(Color.Gray, 1);
            graphics.DrawRectangle(borderPen, borderRectangle);

        }

        private void comboBoxPrintMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            string value = comboBoxPrintMode.SelectedItem.ToString();
            exportToEMFButton.Enabled = value.Equals("Vector", StringComparison.InvariantCultureIgnoreCase);
        }
    }


    class MyHiddenLinesViewPrint : HiddenLinesViewOnPaper
    {
        public MyHiddenLinesViewPrint(HiddenLinesViewSettings data)
            : base(data)
        {

        }

        protected override void  WorkCompleted(Environment model)
        {
 	            // Avoid the automatic printing
        }
    }

}