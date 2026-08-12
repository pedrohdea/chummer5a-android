/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

// Metade dependente de WinForms de SourcebookInfo, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Static/GlobalSettings.cs.
//
// As duas são `partial class SourcebookInfo`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using iText.Kernel.Pdf;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Win32;
using NLog;

namespace Chummer
{
    public static partial class GlobalSettings
    {
        /// <summary>
        /// Converts an image to its Base64 string equivalent with compression settings specified by <see cref="SavedImageQuality"/>.
        /// </summary>
        /// <param name="objImageToSave">Image whose Base64 string should be created.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public static string ImageToBase64StringForStorage(Image objImageToSave, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return SavedImageQuality == int.MaxValue
                ? objImageToSave.ToBase64String(token: token)
                : objImageToSave.ToBase64StringAsJpeg(SavedImageQuality, token);
        }
        /// <summary>
        /// Converts an image to its Base64 string equivalent with compression settings specified by <see cref="SavedImageQuality"/>.
        /// </summary>
        /// <param name="objImageToSave">Image whose Base64 string should be created.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public static Task<string> ImageToBase64StringForStorageAsync(Image objImageToSave, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled<string>(token);
            return SavedImageQuality == int.MaxValue
                ? objImageToSave.ToBase64StringAsync(token: token)
                : objImageToSave.ToBase64StringAsJpegAsync(SavedImageQuality, token: token);
        }
        public static NumericUpDownEx.InterceptMouseWheelMode InterceptMode => AllowHoverIncrement
            ? NumericUpDownEx.InterceptMouseWheelMode.WhenMouseOver
            : NumericUpDownEx.InterceptMouseWheelMode.WhenFocus;
    }
}
