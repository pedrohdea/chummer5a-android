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

// SONDAGEM EXECUTÁVEL DE DEC-037 — carrega .chum5 com o domínio REAL e conta quantos passam.
//
// A pergunta: carregar uma ficha toca algum diálogo de seleção? Os stubs de
// src/Chummer.Compat/DialogStubs.g.cs lançam NotSupportedException com o nome do diálogo,
// e as sombras de scripts/probe/RuntimeShims/ lançam com o nome do membro de WinForms.
// Então a saída deste programa É a resposta: ou o personagem carrega, ou a exceção diz
// exatamente o que falta abstrair.
//
// Deliberadamente NÃO define Utils.IsUnitTest: a suíte legada o usa para suprimir diálogo,
// e suprimir diálogo é precisamente o que invalidaria esta medição.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chummer;
using Chummer.Backend.Attributes;

internal static class CarregarChum5Entry
{
    private const int SegundosPorPersonagem = 300;

    private static readonly string[] s_astrAtributos =
        { "BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG" };

    public static int Main(string[] astrArgs)
    {
        if (astrArgs.Length == 0)
        {
            Console.Error.WriteLine("uso: CarregarChum5 <raiz-do-repo> [filtro ...]");
            return 2;
        }

        string strRaiz = astrArgs[0];
        // Utils.GetStartupPath vem de Application.StartupPath, que na sombra lê esta
        // variável. Precisa estar posta antes do primeiro acesso: o valor é Lazy.
        Environment.SetEnvironmentVariable("CHUMMER_STARTUP_PATH", Path.Combine(strRaiz, "Chummer"));

        InstalarContextoDeTarefas();
        UserInteraction.Current = InteracaoQueAnota.Instancia;

        string strPastaFichas = Path.Combine(strRaiz, "Chummer.Tests", "TestFiles");
        if (!Directory.Exists(strPastaFichas))
        {
            Console.Error.WriteLine("pasta de fichas não encontrada: " + strPastaFichas);
            return 2;
        }

        List<string> lstFiltros = astrArgs.Skip(1)
            .Where(x => !x.StartsWith("-", StringComparison.Ordinal)).ToList();
        // showWarnings=false é o modo de um leitor headless: o domínio resolve sozinho as
        // divergências de configuração em vez de perguntar. É a diferença entre "abre a
        // ficha" e "abre um diálogo".
        bool blnAvisos = !astrArgs.Contains("--sem-avisos");

        List<FileInfo> lstFichas = Directory
            .EnumerateFiles(strPastaFichas, "*.chum5", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(strPastaFichas, "*.chum5lz", SearchOption.AllDirectories))
            .Select(x => new FileInfo(x))
            .Where(x => lstFiltros.Count == 0
                        || lstFiltros.Any(y => x.Name.IndexOf(y, StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderBy(x => x.Length)
            .ToList();

        Console.WriteLine("fichas encontradas: " + lstFichas.Count);
        Console.WriteLine("startup path      : " + Utils.GetStartupPath);
        Console.WriteLine("showWarnings      : " + blnAvisos);
        Console.WriteLine("IsUnitTest        : " + Utils.IsUnitTest + "  (a suíte legada o liga; aqui NÃO)");
        Console.WriteLine();

        int intOk = 0;
        List<string> lstFalhas = new List<string>();

        foreach (FileInfo objFicha in lstFichas)
        {
            string strRotulo = objFicha.Name + " (" + (objFicha.Length / 1024) + " KiB)";
            InteracaoQueAnota.Perguntas.Clear();
            Stopwatch objCronometro = Stopwatch.StartNew();
            string strErro = Carregar(objFicha, blnAvisos, out string strResumo);
            objCronometro.Stop();

            if (strErro == null)
            {
                ++intOk;
                Console.WriteLine("OK    " + strRotulo + "  [" + objCronometro.ElapsedMilliseconds + " ms]");
                Console.WriteLine("      " + strResumo);
            }
            else
            {
                lstFalhas.Add(objFicha.Name + "\t" + strErro);
                Console.WriteLine("FALHA " + strRotulo + "  [" + objCronometro.ElapsedMilliseconds + " ms]");
                Console.WriteLine("      " + strErro.Replace("\n", "\n      "));
            }

            foreach (string strPergunta in InteracaoQueAnota.Perguntas.Distinct())
                Console.WriteLine("      PERGUNTA AO USUÁRIO: " + strPergunta);

            Console.Out.Flush();
        }

        Console.WriteLine();
        Console.WriteLine("=================================================================");
        Console.WriteLine("CARREGARAM: " + intOk + " de " + lstFichas.Count);
        Console.WriteLine("=================================================================");

        if (lstFalhas.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Falhas agrupadas por causa:");
            foreach (IGrouping<string, string> objGrupo in lstFalhas
                         .GroupBy(x => PrimeiraLinha(x.Substring(x.IndexOf('\t') + 1)))
                         .OrderByDescending(x => x.Count()))
            {
                Console.WriteLine();
                Console.WriteLine("  [" + objGrupo.Count() + "x] " + objGrupo.Key);
                foreach (string strItem in objGrupo)
                    Console.WriteLine("        " + strItem.Substring(0, strItem.IndexOf('\t')));
            }
        }

        return intOk == lstFichas.Count ? 0 : 1;
    }

    /// <summary>
    /// ACHADO DO SPIKE, contornado aqui por reflexão para não alterar o domínio.
    ///
    /// Utils.CreateSynchronizationContext exige que a thread seja STA. Apartamento COM é
    /// conceito do Windows: fora dele, Thread.GetApartmentState devolve Unknown e
    /// Thread.SetApartmentState(STA) lança PlatformNotSupportedException. Ou seja, o
    /// primeiro `new Character()` morre antes de tocar em qualquer XML.
    ///
    /// A sondagem instala um JoinableTaskContext direto no campo estático. Assim
    /// MyJoinableTaskContext deixa de ser nulo e CreateSynchronizationContext nunca é
    /// chamado. O domínio fica intocado — a correção de verdade é da Etapa de porte.
    /// </summary>
    private static void InstalarContextoDeTarefas()
    {
        System.Reflection.FieldInfo objCampo = typeof(Utils).GetField(
            "s_objJoinableTaskContext",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (objCampo == null)
            throw new InvalidOperationException(
                "Utils.s_objJoinableTaskContext sumiu; a sondagem precisa ser revista.");
        objCampo.SetValue(null, new Microsoft.VisualStudio.Threading.JoinableTaskContext());
    }

    /// <summary>
    /// A fachada UserInteraction já existe e, sem implementação instalada, responde OK em
    /// silêncio. Isso é bom para não travar, e péssimo para esta medição: uma pergunta feita
    /// ao usuário durante a carga passaria despercebida. Esta implementação ANOTA cada
    /// pergunta e segue devolvendo o padrão, para que a saída do spike mostre o que o
    /// domínio teria perguntado.
    /// </summary>
    internal sealed class InteracaoQueAnota : IUserInteraction
    {
        public static readonly InteracaoQueAnota Instancia = new InteracaoQueAnota();

        public static readonly List<string> Perguntas = new List<string>();

        private static PromptResult Anotar(string strTipo, string strMensagem, PromptButtons eBotoes)
        {
            string strLinha = strTipo + " [" + eBotoes + "] " + PrimeiraLinha(strMensagem ?? string.Empty);
            Perguntas.Add(strLinha);
            return UserInteraction.DefaultResult;
        }

        public PromptResult ShowMessage(string message, string caption, PromptButtons buttons,
            PromptIcon icon, PromptDefaultButton defaultButton)
            => Anotar("ShowMessage", message, buttons);

        public PromptResult ShowScrollableMessage(string message, string caption, PromptButtons buttons,
            PromptIcon icon, PromptDefaultButton defaultButton)
            => Anotar("ShowScrollableMessage", message, buttons);

        public Task<PromptResult> ShowMessageAsync(string message, string caption, PromptButtons buttons,
            PromptIcon icon, PromptDefaultButton defaultButton, CancellationToken token)
            => Task.FromResult(Anotar("ShowMessageAsync", message, buttons));

        public Task<PromptResult> ShowScrollableMessageAsync(string message, string caption,
            PromptButtons buttons, PromptIcon icon, PromptDefaultButton defaultButton, CancellationToken token)
            => Task.FromResult(Anotar("ShowScrollableMessageAsync", message, buttons));
    }

    private static string PrimeiraLinha(string strTexto)
    {
        int intQuebra = strTexto.IndexOf('\n');
        return intQuebra < 0 ? strTexto : strTexto.Substring(0, intQuebra);
    }

    /// <summary>
    /// Devolve null se carregou, ou a descrição da exceção. O resumo prova que a carga
    /// aconteceu de verdade: sem ler nome, metatipo e atributos, "OK" não significa nada.
    /// </summary>
    private static string Carregar(FileInfo objFicha, bool blnAvisos, out string strResumo)
    {
        strResumo = string.Empty;
        Character objPersonagem = null;
        try
        {
            using (CancellationTokenSource objFonte
                   = new CancellationTokenSource(TimeSpan.FromSeconds(SegundosPorPersonagem)))
            {
                objPersonagem = new Character
                {
                    FileName = objFicha.FullName
                };
                if (!objPersonagem.Load(showWarnings: blnAvisos, token: objFonte.Token))
                    return "Load devolveu false (sem exceção)";

                strResumo = Resumir(objPersonagem, objFonte.Token);
                return null;
            }
        }
        catch (Exception e)
        {
            return e.GetType().Name + ": " + e.Message + "\n" + PrimeirosQuadros(e);
        }
        finally
        {
            objPersonagem?.Dispose();
        }
    }

    private static string Resumir(Character objPersonagem, CancellationToken token)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('"').Append(objPersonagem.Name).Append("\"  metatipo=").Append(objPersonagem.Metatype);
        if (!string.IsNullOrEmpty(objPersonagem.Metavariant))
            sb.Append('/').Append(objPersonagem.Metavariant);
        sb.Append("  criado=").Append(objPersonagem.Created);
        sb.Append("  karma=").Append(objPersonagem.Karma.ToString(CultureInfo.InvariantCulture));
        sb.Append("  nuyen=").Append(objPersonagem.Nuyen.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine();
        sb.Append("      ");
        foreach (string strAbrev in s_astrAtributos)
        {
            CharacterAttrib objAtributo = objPersonagem.GetAttribute(strAbrev, token: token);
            if (objAtributo == null)
                continue;
            sb.Append(strAbrev).Append('=').Append(objAtributo.TotalValue.ToString(CultureInfo.InvariantCulture))
              .Append("  ");
        }

        sb.AppendLine();
        sb.Append("      qualidades=").Append(objPersonagem.Qualities.Count)
          .Append("  perícias=").Append(objPersonagem.SkillsSection.Skills.Count)
          .Append("  magias=").Append(objPersonagem.Spells.Count)
          .Append("  armas=").Append(objPersonagem.Weapons.Count)
          .Append("  cyberware=").Append(objPersonagem.Cyberware.Count)
          .Append("  equipamento=").Append(objPersonagem.Gear.Count);
        return sb.ToString();
    }

    private static string PrimeirosQuadros(Exception e)
    {
        string strPilha = e.StackTrace ?? string.Empty;
        string[] astrLinhas = strPilha.Split('\n');
        return string.Join("\n", astrLinhas.Take(6).Select(x => x.TrimEnd()));
    }
}
