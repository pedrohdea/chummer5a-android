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

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Chummer
{
    /// <summary>
    /// Something that carries portraits ("mugshots").
    /// </summary>
    /// <remarks>
    /// Portraits are held as the raw bytes of the encoded image file (PNG, JPEG, ...) — the very bytes the Base64 in a
    /// .chum5 decodes to. The domain neither decodes nor re-encodes them: it loads, keeps and hands them back, so saving
    /// writes out exactly what loading read and no generation loss accumulates. Decoding into a platform image type is
    /// the presentation layer's job, which is what keeps this interface free of System.Drawing (DEC-034).
    /// </remarks>
    public interface IHasMugshots : IDisposable, IAsyncDisposable
    {
        ThreadSafeList<byte[]> Mugshots { get; }
        byte[] MainMugshot { get; set; }
        int MainMugshotIndex { get; set; }

        Task<ThreadSafeList<byte[]>> GetMugshotsAsync(CancellationToken token = default);

        Task<byte[]> GetMainMugshotAsync(CancellationToken token = default);

        Task SetMainMugshotAsync(byte[] value, CancellationToken token = default);

        Task<int> GetMainMugshotIndexAsync(CancellationToken token = default);

        Task SetMainMugshotIndexAsync(int value, CancellationToken token = default);

        Task ModifyMainMugshotIndexAsync(int value, CancellationToken token = default);

        void SaveMugshots(XmlWriter objWriter, CancellationToken token = default);

        Task SaveMugshotsAsync(XmlWriter objWriter, CancellationToken token = default);

        void LoadMugshots(XPathNavigator xmlSavedNode, CancellationToken token = default);

        Task LoadMugshotsAsync(XPathNavigator xmlSavedNode, CancellationToken token = default);

        Task PrintMugshots(XmlWriter objWriter, CancellationToken token = default);
    }
}
