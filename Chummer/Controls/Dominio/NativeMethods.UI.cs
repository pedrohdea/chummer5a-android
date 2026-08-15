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

// Metade dependente de WinForms de NativeMethods, separada do domínio durante a
// extração do núcleo (DEC-023). A metade de regras vive em Chummer/Backend/Static/NativeMethods.cs.
//
// As duas são `partial class NativeMethods`: no projeto legado voltam a ser uma classe
// só, então nenhum chamador precisou mudar. No Chummer.Core, só o domínio existe.

using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Chummer
{
    internal static partial class NativeMethods
    {
        /// <summary>
        /// Gets a Windows stock icon. Useful as an alternative to the SystemIcons class.
        /// </summary>
        /// <param name="eIconId">Id to indicate which stock icon to fetch.</param>
        internal static Icon GetStockIcon(SHSTOCKICONID eIconId)
        {
            SHSTOCKICONINFO sii = new SHSTOCKICONINFO
            {
                cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>()
            };
            Marshal.ThrowExceptionForHR(SHGetStockIconInfo(eIconId, SHGSI.SHGSI_ICON, ref sii));
            try
            {
                return Icon.FromHandle(sii.hIcon);
            } // However, Icon.FromHandle is semi-stub in WINE so here is a backup.
            catch (ArgumentException)
            {
                switch (eIconId)
                {
                    case SHSTOCKICONID.SIID_APPLICATION:
                        return SystemIcons.Application;
                    case SHSTOCKICONID.SIID_ERROR:
                        return SystemIcons.Error;
                    case SHSTOCKICONID.SIID_WARNING:
                        return SystemIcons.Warning;
                    case SHSTOCKICONID.SIID_HELP:
                        return SystemIcons.Question;
                    case SHSTOCKICONID.SIID_INFO:
                        return SystemIcons.Information;
                    case SHSTOCKICONID.SIID_SHIELD:
                        return SystemIcons.Shield;
                    default:
                        return SystemIcons.Exclamation;
                }
            }
        }
    }
}
