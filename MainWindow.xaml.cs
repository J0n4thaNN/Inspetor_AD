using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.DirectoryServices.AccountManagement;
using MonitorUsuariosAD.Models; // PASSO 1: Adicionar referência à pasta Models

namespace MonitorUsuariosAD
{
    public partial class MainWindow : Window
    {
        // A classe interna "ResultadoConsulta" foi REMOVIDA daqui.

        private CancellationTokenSource? _cts;

        public MainWindow()
        {
            InitializeComponent();
            // Adicione um botão chamado BtnTestarDNS no seu XAML e descomente a linha abaixo
            // BtnTestarDNS.Click += BtnTestarDNS_OnClick;
        }

        // --- INÍCIO DO CÓDIGO DE TESTE ---

        // Método de clique para o botão de teste de DNS
        private void BtnTestarDNS_OnClick(object sender, RoutedEventArgs e)
        {
            // Pega o primeiro alvo da caixa de texto para o teste
            string alvo = ParseAlvos(TxtAlvos.Text).FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(alvo))
            {
                MessageBox.Show("Por favor, insira um endereço IP na caixa de texto para testar.", "Teste de DNS", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Verifica se o alvo é um IP válido
            if (!IPAddress.TryParse(alvo, out IPAddress? ipAddress))
            {
                MessageBox.Show($"'{alvo}' não é um endereço IP válido.", "Teste de DNS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Tenta resolver o IP para um nome de host
                IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
                string hostname = hostEntry.HostName;
                MessageBox.Show($"O IP '{alvo}' foi resolvido para o hostname: '{hostname}'.", "Teste de DNS - Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Se a resolução falhar, mostra a mensagem de erro
                MessageBox.Show($"Falha ao resolver o IP '{alvo}'.\n\nErro: {ex.Message}", "Teste de DNS - Falha", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- FIM DO CÓDIGO DE TESTE ---

        private void AlternarEstadoBotoes(bool executando)
        {
            BtnConsultar.IsEnabled = !executando;
            BtnConsultarAD.IsEnabled = !executando;
            BtnConsultarADOnline.IsEnabled = !executando;
            BtnCancelar.IsEnabled = executando;
            BarraProgresso.Visibility = executando ? Visibility.Visible : Visibility.Collapsed;
            TxtStatusBar.Text = executando ? "Consultando..." : "Pronto";
        }

        private void BtnCancelar_OnClick(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            BtnCancelar.IsEnabled = false;
            TxtStatusBar.Text = "Cancelando...";
        }

        private static IEnumerable<string> ParseAlvos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return Enumerable.Empty<string>();
            var separadores = new[] { '\r', '\n', ',', ';', ' ' };
            return texto
                .Split(separadores, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        // PASSO 2: Método de consulta agora também busca o LastLogon
        private static ResultadoConsulta? ConsultarUsuario(string alvo, CancellationToken token)
        {
            if (token.IsCancellationRequested) return null;

            var resultado = new ResultadoConsulta
            {
                Alvo = alvo,
                Usuario = "-",
                Status = "OFFLINE" // Alterado para OFFLINE para mais clareza
            };

            try
            {
                using var ping = new Ping();
                var reply = ping.Send(alvo, 1000);
                if (reply.Status != IPStatus.Success) return resultado;

                if (token.IsCancellationRequested) return null;

                // Busca o LastLogon ANTES de tentar a conexão WMI
                try
                {
                    string nomeComputador = alvo;
                    // Se 'alvo' for um IP, tenta resolver para um hostname
                    if (IPAddress.TryParse(alvo, out IPAddress? ipAddress))
                    {
                        try
                        {
                            IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
                            nomeComputador = hostEntry.HostName.Split('.')[0]; // Usa apenas o nome curto
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Falha na resolução DNS para '{alvo}': {ex.Message}");
                        }
                    }

                    using (var context = new PrincipalContext(ContextType.Domain))
                    {
                        ComputerPrincipal computer = ComputerPrincipal.FindByIdentity(context, nomeComputador);
                        if (computer != null && computer.LastLogon.HasValue)
                        {
                            // CORREÇÃO: Converte a data/hora de UTC para o horário local
                            resultado.LastLogon = computer.LastLogon.Value.ToLocalTime();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Adiciona um log para ver o erro que está sendo ignorado
                    Debug.WriteLine($"Falha ao buscar LastLogon para '{alvo}': {ex.Message}");
                    // Ignora erros ao buscar LastLogon, pode não estar no domínio.
                }


                var options = new ConnectionOptions { Timeout = TimeSpan.FromSeconds(3) };
                var scope = new ManagementScope($@"\\{alvo}\root\cimv2", options);
                scope.Connect();

                if (token.IsCancellationRequested) return null;

                var query = new ObjectQuery("SELECT UserName FROM Win32_ComputerSystem");
                using var searcher = new ManagementObjectSearcher(scope, query);
                using var results = searcher.Get();

                string? userName = null;
                foreach (ManagementObject mo in results)
                {
                    userName = mo["UserName"] as string;
                    break;
                }

                resultado.Usuario = string.IsNullOrWhiteSpace(userName) ? "Ninguém logado" : userName;
                resultado.Status = "ONLINE";
            }
            catch (UnauthorizedAccessException) { resultado.Status = "Acesso Negado"; }
            catch (System.Runtime.InteropServices.COMException) { resultado.Status = "Erro RPC/WMI"; }
            catch (Exception) { resultado.Status = "Erro de Conexão"; }

            return resultado;
        }

        private async void BtnConsultar_OnClick(object sender, RoutedEventArgs e)
        {
            var alvos = ParseAlvos(TxtAlvos.Text);
            if (!alvos.Any())
            {
                MessageBox.Show("Informe pelo menos um IP ou hostname.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AlternarEstadoBotoes(true);
            GridResultados.ItemsSource = null;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var resultados = new List<ResultadoConsulta>();
                using var sem = new SemaphoreSlim(10);

                var tasks = alvos.Select(async alvo =>
                {
                    if (token.IsCancellationRequested) return;

                    try
                    {
                        await sem.WaitAsync(token);
                        try
                        {
                            var resultado = await Task.Run(() => ConsultarUsuario(alvo, token), token);
                            if (resultado != null)
                            {
                                lock (resultados) { resultados.Add(resultado); }
                            }
                        }
                        finally { sem.Release(); }
                    }
                    catch (OperationCanceledException) { }
                });

                try { await Task.WhenAll(tasks); } catch (OperationCanceledException) { }

                GridResultados.ItemsSource = resultados.OrderBy(r => r.Alvo, StringComparer.OrdinalIgnoreCase).ToList();
                TxtStatusBar.Text = $"Consulta concluída. {resultados.Count} resultados.";
            }
            finally
            {
                AlternarEstadoBotoes(false);
            }
        }

        private async void BtnConsultarAD_OnClick(object sender, RoutedEventArgs e)
        {
            AlternarEstadoBotoes(true);
            GridResultados.ItemsSource = null;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var alvos = new List<string>();

                await Task.Run(() =>
                {
                    using var context = new PrincipalContext(ContextType.Domain);
                    using var searcher = new PrincipalSearcher(new ComputerPrincipal(context));
                    foreach (var result in searcher.FindAll())
                    {
                        if (token.IsCancellationRequested) break;
                        if (result is ComputerPrincipal computer && !string.IsNullOrWhiteSpace(computer.Name))
                        {
                            alvos.Add(computer.Name);
                        }
                    }
                }, token);

                if (token.IsCancellationRequested) return;

                if (!alvos.Any())
                {
                    MessageBox.Show("Nenhum computador encontrado no domínio.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtAlvos.Text = string.Join(Environment.NewLine, alvos);
                var resultados = new List<ResultadoConsulta>();
                using var sem = new SemaphoreSlim(30);

                var tasks = alvos.Select(async alvo =>
                {
                    if (token.IsCancellationRequested) return;

                    try
                    {
                        await sem.WaitAsync(token);
                        try
                        {
                            var resultado = await Task.Run(() => ConsultarUsuario(alvo, token), token);
                            if (resultado != null)
                            {
                                lock (resultados) { resultados.Add(resultado); }
                            }
                        }
                        finally { sem.Release(); }
                    }
                    catch (OperationCanceledException) { }
                });

                try { await Task.WhenAll(tasks); } catch (OperationCanceledException) { }

                GridResultados.ItemsSource = resultados.OrderBy(r => r.Alvo, StringComparer.OrdinalIgnoreCase).ToList();
                TxtStatusBar.Text = $"Consulta concluída. {resultados.Count} resultados.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AlternarEstadoBotoes(false);
            }
        }

        private async void BtnConsultarADOnline_OnClick(object sender, RoutedEventArgs e)
        {
            AlternarEstadoBotoes(true);
            GridResultados.ItemsSource = null;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                var alvos = new List<string>();

                await Task.Run(() =>
                {
                    using var context = new PrincipalContext(ContextType.Domain);
                    using var searcher = new PrincipalSearcher(new ComputerPrincipal(context));
                    foreach (var result in searcher.FindAll())
                    {
                        if (token.IsCancellationRequested) break;
                        if (result is ComputerPrincipal computer && !string.IsNullOrWhiteSpace(computer.Name))
                        {
                            alvos.Add(computer.Name);
                        }
                    }
                }, token);

                if (token.IsCancellationRequested) return;

                if (!alvos.Any())
                {
                    MessageBox.Show("Nenhum computador encontrado no domínio.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtAlvos.Text = string.Join(Environment.NewLine, alvos);
                var resultados = new List<ResultadoConsulta>();
                using var sem = new SemaphoreSlim(30);

                var tasks = alvos.Select(async alvo =>
                {
                    if (token.IsCancellationRequested) return;

                    try
                    {
                        await sem.WaitAsync(token);
                        try
                        {
                            var resultado = await Task.Run(() => ConsultarUsuario(alvo, token), token);
                            if (resultado != null && resultado.Status != "OFFLINE")
                            {
                                lock (resultados) { resultados.Add(resultado); }
                            }
                        }
                        finally { sem.Release(); }
                    }
                    catch (OperationCanceledException) { }
                });

                try { await Task.WhenAll(tasks); } catch (OperationCanceledException) { }

                GridResultados.ItemsSource = resultados.OrderBy(r => r.Alvo, StringComparer.OrdinalIgnoreCase).ToList();
                TxtStatusBar.Text = $"Consulta concluída. {resultados.Count} resultados.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AlternarEstadoBotoes(false);
            }
        }
    }
}