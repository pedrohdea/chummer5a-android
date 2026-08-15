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
        /// Encodes an image into the bytes that will be stored for it, with compression settings specified by <see cref="SavedImageQuality"/>.
        /// Called when the user picks an image, not when the character is saved: the domain stores portraits as bytes and writes back
        /// exactly what it read, so compression belongs at ingestion and happens once instead of on every save (DEC-034).
        /// </summary>
        /// <param name="objImageToSave">Image whose stored bytes should be created.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public static byte[] ImageToBytesForStorage(Image objImageToSave, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return SavedImageQuality == int.MaxValue
                ? objImageToSave.ToBytes(token: token)
                : objImageToSave.ToBytesAsJpeg(SavedImageQuality, token);
        }
        /// <summary>
        /// Encodes an image into the bytes that will be stored for it, with compression settings specified by <see cref="SavedImageQuality"/>.
        /// Called when the user picks an image, not when the character is saved: the domain stores portraits as bytes and writes back
        /// exactly what it read, so compression belongs at ingestion and happens once instead of on every save (DEC-034).
        /// </summary>
        /// <param name="objImageToSave">Image whose stored bytes should be created.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public static Task<byte[]> ImageToBytesForStorageAsync(Image objImageToSave, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled<byte[]>(token);
            return SavedImageQuality == int.MaxValue
                ? objImageToSave.ToBytesAsync(token: token)
                : objImageToSave.ToBytesAsJpegAsync(SavedImageQuality, token: token);
        }
        public static NumericUpDownEx.InterceptMouseWheelMode InterceptMode => AllowHoverIncrement
            ? NumericUpDownEx.InterceptMouseWheelMode.WhenMouseOver
            : NumericUpDownEx.InterceptMouseWheelMode.WhenFocus;
    }
}
