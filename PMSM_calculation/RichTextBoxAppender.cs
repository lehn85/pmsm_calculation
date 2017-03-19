/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.Text;
using log4net.Appender;
using log4net.Layout;
using log4net.Util;
using System.Windows.Forms;
using log4net.Core;
using System.IO;

namespace calc_from_geometryOfMotor
{
    class RichTextBoxAppender : AppenderSkeleton
    {
        public static RichTextBox mRichTextBox;

        public RichTextBoxAppender()
        {

        }

        override protected void Append(LoggingEvent loggingEvent)
        {
            if (mRichTextBox == null || mRichTextBox.IsDisposed)
                return;

            string message = RenderLoggingEvent(loggingEvent);

            try
            {
                mRichTextBox.BeginInvoke(new MethodInvoker(delegate()
                {
                    if (!mRichTextBox.IsDisposed)
                        mRichTextBox.AppendText(message);
                }));
            }
            catch
            {
            }
        }
    }
}
