using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AutomatoFinito
{
    public class AutomatoFinito
    {
        public List<Estado> estados;
        public List<char> alfabeto;

        public AutomatoFinito()
        {
            this.estados = new List<Estado>();
            this.alfabeto = new List<char>();
        }

        public void ordenaAlfabeto()
        {
            this.alfabeto.Sort(
              delegate (char x, char y) {
                  return x.CompareTo(y);
              }
            );
        }

        public string criaListaSaida()
        {
            StringBuilder saida = new StringBuilder();
            saida.Append("------------------------------------\n");
            foreach (var estado in estados)
            {
                saida.AppendLine("Estado (" + (estado.final ? "*" : "") + estado.nome + ")");
                List<Estado> estadosDistintos = estado.transicoes.Select(x => x.estadoDestino).Distinct().ToList(); ;
                foreach (var destino in estadosDistintos)
                {
                    saida.AppendLine(string.Join(",", 
                                     estado.transicoes.Where(x => x.estadoDestino.nome.Equals(destino.nome)).Select(x => x.simbolo)) +
                                     " -> " + destino.nome);
                }
                saida.Append("------------------------------------\n");
            }
            return saida.ToString();
        }

        public string ToCsv(string separator)
        {
            StringBuilder saida = new StringBuilder();
            List<string[]> listaSaida = this.criaListaSaidaCsv();

            // Transforma a lista em csv
            foreach (string[] linha in listaSaida)
            {
                saida.AppendLine(String.Join(separator, linha));
            }
            return saida.ToString();
        }

        private List<string[]> criaListaSaidaCsv()
        {
            List<string[]> listaSaida = new List<string[]>();
            // Monta o cabeçalho
            this.ordenaAlfabeto();
            listaSaida.Add(Enumerable.Repeat<string>("", this.alfabeto.Count + 1).ToArray());
            listaSaida[0][0] = "δ";
            for (int i = 0; i < this.alfabeto.Count(); i++)
            {
                listaSaida[0][i + 1] = this.alfabeto[i].ToString();
            }

            // Monta cada linha do autômato 
            foreach (var estado in this.estados)
            {
                listaSaida.Add(Enumerable.Repeat<string>("-", this.alfabeto.Count + 1).ToArray());
                listaSaida[listaSaida.Count - 1][0] = (estado.nome == "0" ? "->" : "") + (estado.final ? "*" : "") + estado.nome;
                foreach (var transicao in estado.transicoes)
                {
                    int indiceLinha = listaSaida.Count() - 1;
                    int indiceColuna = this.alfabeto.IndexOf(transicao.simbolo) + 1;
                    if (listaSaida[indiceLinha][indiceColuna].Equals("-"))
                        listaSaida[indiceLinha][indiceColuna] = transicao.estadoDestino.nome;
                    else
                        listaSaida[indiceLinha][indiceColuna] += "," + transicao.estadoDestino.nome;
                }
            }
            return listaSaida;
        }

    }

    public class ViewAutomatoFinito 
    {

        public string arquivoEntrada, arquivoSaidaCSV;
        List<string> listaEntrada = new List<string>();
        
        public AutomatoFinito CarregarAutomatoFinito()
        {
            lerArquivo();
            AutomatoFinito automatoFinito = gerarAutomatoFinitoInicial();
            automatoFinito = determinizarAutomato(automatoFinito);
            removerMortos(automatoFinito);
            escreverArquivoCSV(automatoFinito);

            return automatoFinito;
        }

        private void escreverArquivoCSV(AutomatoFinito automatoFinito)
        {
            if (Path.GetExtension(this.arquivoSaidaCSV).Contains("csv"))
            {
                StreamWriter stream = new StreamWriter(this.arquivoSaidaCSV);
                stream.Write(automatoFinito.ToCsv(";"));
                stream.Close();
                stream.Dispose();
            }
        }

        private void lerArquivo()
        {
            StreamReader leitorEntrada = new System.IO.StreamReader(arquivoEntrada);
            while (!leitorEntrada.EndOfStream)
            {
                string linha = leitorEntrada.ReadLine();
                if (!string.IsNullOrEmpty(linha))
                {
                    listaEntrada.Add(linha);
                }
            }
            leitorEntrada.Close();
            leitorEntrada.Dispose();
        }
        
        private AutomatoFinito gerarAutomatoFinitoInicial()
        {
            List<string> producoes, tokens;
            producoes = (from linha in listaEntrada where linha.Contains(Utils.simboloAtribuicao) select linha.Replace(" ", string.Empty)).ToList();
            listaEntrada.RemoveAll(x => producoes.Contains(x.Replace(" ", string.Empty)));
            tokens = (from linha in listaEntrada select linha.Replace(" ", string.Empty)).ToList();

            AutomatoFinito automatoFinitoInicial = new AutomatoFinito();
            analisarProducoes(producoes, automatoFinitoInicial);
            analisarTokens(tokens, automatoFinitoInicial);

            return automatoFinitoInicial;
        }

        private void analisarProducoes(List<string> producoes, AutomatoFinito automatoFinito)
        {
            int indiceDoSeparador;
            string[] transicoes;
            char simbolo;
            Estado estadoAtual;
            Dictionary<string, string> mapeamentoEstados = new Dictionary<string, string>();
            
            automatoFinito.estados.Add(new Estado((0).ToString(), false)); // primeiro estado: S
            foreach (var producao in producoes)
            {
                indiceDoSeparador = producao.IndexOf(Utils.simboloAtribuicao);
                string labelEstado = producao.Substring(0, indiceDoSeparador).Replace("<", String.Empty).Replace(">", String.Empty);
                if (labelEstado.Equals("S"))
                { // indica que uma nova gramatica ta comecando
                    mapeamentoEstados.Clear();
                    mapeamentoEstados.Add("S", (0).ToString());
                }
                if (mapeamentoEstados.ContainsKey(labelEstado))
                { // se o estado ja ta mapeado busca o estado
                    string valor;
                    mapeamentoEstados.TryGetValue(labelEstado, out valor);
                    estadoAtual = (from obj in automatoFinito.estados where obj.nome.Equals(valor) select obj).FirstOrDefault();
                }
                else
                { // senao mapeia e cria um estado novo
                    int idEstado = automatoFinito.estados.Count();
                    mapeamentoEstados.Add(labelEstado, idEstado.ToString());
                    estadoAtual = new Estado(idEstado.ToString(), false);
                    automatoFinito.estados.Add(estadoAtual);
                }

                transicoes = producao.Substring(indiceDoSeparador + Utils.simboloAtribuicao.Length).Replace("<", String.Empty).Replace(">", String.Empty).Split('|');
                foreach (var transicao in transicoes)
                {
                    // transicao(0) é simbolo
                    // transicao(1), se tiver, é estado, se não tiver cria um final sem transição
                    // se o estado nao ta no mapeamento, cria um e poe no map
                    simbolo = transicao[0];
                    if (simbolo.Equals(Utils.epsolon))
                    { // se o símbolo é o epsolon, o estado é final
                        estadoAtual.final = true;
                    }
                    else
                    {
                        Estado estadoDestino;
                        if (simbolo.ToString().Equals(transicao))
                        { // se só tem um símbolo, sem transição, cria um novo estado final
                            int idEstado = automatoFinito.estados.Count();
                            //mapeamentoEstados.Add(labelEstado, idEstado); não mapeia pq não vai ter outra referencia para este estado
                            estadoDestino = new Estado(idEstado.ToString(), true);
                            automatoFinito.estados.Add(estadoDestino);
                        }
                        else
                        {
                            // se tem simbolo e transição para outro estado
                            // verifica se o estado existe, senao cria um e mapeia
                            string lblEstadoTransicao = transicao.Substring(1);
                            if (mapeamentoEstados.ContainsKey(lblEstadoTransicao))
                            { // se o estado ja ta mapeado busca o estado
                                string valor;
                                mapeamentoEstados.TryGetValue(lblEstadoTransicao, out valor);
                                estadoDestino = (from obj in automatoFinito.estados where obj.nome.Equals(valor) select obj).FirstOrDefault();
                            }
                            else
                            { // senao mapeia e cria um estado novo
                                int idEstado = automatoFinito.estados.Count();
                                mapeamentoEstados.Add(lblEstadoTransicao, idEstado.ToString());
                                estadoDestino = new Estado(idEstado.ToString(), false);
                                automatoFinito.estados.Add(estadoDestino);
                            }
                        }
                        estadoAtual.transicoes.Add(new Transicao(simbolo, estadoDestino));
                        if (!automatoFinito.alfabeto.Contains(simbolo))
                        {
                            automatoFinito.alfabeto.Add(simbolo);
                        }
                    }
                }
            }
        }

        private void analisarTokens(List<string> tokens, AutomatoFinito automatoFinito)
        {
            if (automatoFinito.estados.Count == 0)
            { // se não tem produções, só tokens
                automatoFinito.estados.Add(new Estado((0).ToString(), false)); // primeiro estado: S
            }
            foreach (var token in tokens)
            {
                Estado estadoAtual = automatoFinito.estados[0]; // estado S
                foreach (var simbolo in token)
                {
                    // cria um novo estado para transição
                    int idEstado = automatoFinito.estados.Count();
                    Estado estadoNovo = new Estado(idEstado.ToString(), false);
                    automatoFinito.estados.Add(estadoNovo);
                    // cria a transição para o novo estado
                    estadoAtual.transicoes.Add(new Transicao(simbolo, estadoNovo));
                    if (!automatoFinito.alfabeto.Contains(simbolo))
                    {
                        automatoFinito.alfabeto.Add(simbolo);
                    }
                    estadoAtual = estadoNovo;
                }
                estadoAtual.final = true;
            }
        }

        private AutomatoFinito determinizarAutomato(AutomatoFinito automatoFinito)
        {
            AutomatoFinito automatoFinitoDeterministico = new AutomatoFinito();
            List<Estado> estadosPendentes = new List<Estado>();
            List<Estado> estadosCombinados = new List<Estado>();
            List<Transicao> transicoes = new List<Transicao>();
            
            automatoFinitoDeterministico.alfabeto = automatoFinito.alfabeto;
            estadosPendentes.Add(automatoFinito.estados[0]);
            while (estadosPendentes.Count > 0)
            {
                List<string> labelsCombinados = estadosPendentes[0].nome.Split(',').ToList();
                estadosCombinados = (from obj in automatoFinito.estados where labelsCombinados.Contains(obj.nome) select obj).ToList();

                Estado novoEstado = (from obj in automatoFinitoDeterministico.estados where obj.nome.Equals(estadosPendentes[0].nome) select obj).FirstOrDefault();
                if (novoEstado == null)
                {
                    novoEstado = new Estado(estadosPendentes[0].nome, estadosCombinados.Exists(x => x.final));
                    automatoFinitoDeterministico.estados.Add(novoEstado);
                }
                else
                {
                    novoEstado.final = estadosCombinados.Exists(x => x.final);
                }

                foreach (var estado in estadosCombinados)
                {
                    transicoes.AddRange(estado.transicoes);
                }

                List<char> simbolosDistintos = transicoes.Select(x => x.simbolo).Distinct().ToList();
                simbolosDistintos.Sort();
                foreach (var simbolo in simbolosDistintos)
                {
                    List<string> estadosAlcancadosPeloSimbolo = transicoes.Where(x => x.simbolo.Equals(simbolo)).Select(x => x.estadoDestino.nome).Distinct().ToList();
                    estadosAlcancadosPeloSimbolo.Sort();
                    string labelEstadoDestino = string.Join(",", estadosAlcancadosPeloSimbolo);
                    Estado estadoDestino = (from obj in automatoFinitoDeterministico.estados where obj.nome.Equals(labelEstadoDestino) select obj).FirstOrDefault();
                    if (estadoDestino == null)
                    {
                        estadoDestino = new Estado(labelEstadoDestino, false);
                        automatoFinitoDeterministico.estados.Add(estadoDestino);
                        estadosPendentes.Add(estadoDestino);
                    }
                    novoEstado.transicoes.Add(new Transicao(simbolo, estadoDestino));
                }

                transicoes.Clear();
                estadosPendentes.RemoveAt(0);
            }
            return automatoFinitoDeterministico;
        }

        private void removerMortos(AutomatoFinito automatoFinito)
        {
            List<Estado> estadosRemover = new List<Estado>();
            foreach (Estado estado in automatoFinito.estados)
            {
                if (estado.final)
                {
                    continue;
                }
                bool vivo = BuscaLargura(automatoFinito, estado);
                if (!vivo)
                {
                    estadosRemover.Add(estado);
                }
            }
            automatoFinito.estados.RemoveAll(x => estadosRemover.Contains(x));
            foreach (Estado estado in automatoFinito.estados)
            {
                estado.transicoes = estado.transicoes.Where(x => automatoFinito.estados.Contains(x.estadoDestino)).ToList();
            }
        }

        private bool BuscaLargura(AutomatoFinito automatoFinito, Estado estadoInicial)
        {
            // retorna true se encontra um estado final, senão false
            Queue<Estado> fila = new Queue<Estado>();
            List<Estado> estadosAlcancados = new List<Estado>();
            fila.Enqueue(estadoInicial);
            while (fila.Count > 0)
            {
                Estado estadoDesempilhado = fila.Dequeue();
                foreach (Transicao transicao in estadoDesempilhado.transicoes)
                {
                    if (transicao.estadoDestino != null)
                    {
                        if (!estadosAlcancados.Contains(transicao.estadoDestino))
                        {
                            if (transicao.estadoDestino.final)
                            {
                                return true;
                            }
                            fila.Enqueue(transicao.estadoDestino);
                            estadosAlcancados.Add(transicao.estadoDestino);
                        }
                    }
                }
            }
            return false;
        }
    }

    public class Estado
    {
        public string nome;
        public bool final;
        public List<Transicao> transicoes;

        public Estado(string Plabel, bool Pfinal)
        {
            this.nome = Plabel;
            this.final = Pfinal;
            this.transicoes = new List<Transicao>();
        }
    }

    public class Transicao
    {
        public char simbolo;
        public Estado estadoDestino;

        public Transicao(char Psimbolo, Estado PEstadoDestino)
        {
            this.simbolo = Psimbolo;
            this.estadoDestino = PEstadoDestino;
        }

    }

    public class Utils
    {
        public static string simboloAtribuicao = "::=";
        public static char epsolon = '&';

        public static System.Text.RegularExpressions.Regex expressaoOperadores = new System.Text.RegularExpressions.Regex("[:+\\-*\\/=&¬%><]{1}");
        public static System.Text.RegularExpressions.Regex expressaoEspacosDuplos = new System.Text.RegularExpressions.Regex("[ ]{2,}");
    }

    class Program
    {
        static void Main(string[] args)
        {            
            string txtEntradaGramatica, txtSaidaAutomato, txtCodigoFonte, txtSaidaAutomatoCSV;

            txtEntradaGramatica = @"Entrada\entrada.txt";
            txtSaidaAutomato = @"Saida\saida.txt";
            txtSaidaAutomatoCSV = @"Automato\sai.csv";
            txtCodigoFonte = @"Entrada\fonte.txt";

            StreamWriter arquivoSaida = new StreamWriter(txtSaidaAutomato, false, Encoding.UTF8);

            ViewAutomatoFinito CriadorAutomato = new ViewAutomatoFinito();
            AutomatoFinito automatoFinito = new AutomatoFinito();

            CriadorAutomato.arquivoEntrada = txtEntradaGramatica;
            CriadorAutomato.arquivoSaidaCSV = txtSaidaAutomatoCSV;
            automatoFinito = CriadorAutomato.CarregarAutomatoFinito();

            Console.WriteLine("Autômato finito gerado.");
            arquivoSaida.WriteLine("Autômato finito gerado.");
            arquivoSaida.WriteLine(automatoFinito.criaListaSaida());

            AnalisadorLexico Lexico = new AnalisadorLexico();
            List<TabelaSimbolos> tokensLidos = new List<TabelaSimbolos>();
            List<string> erros = new List<string>();
            Lexico.analisarConteudo(txtCodigoFonte, automatoFinito, ref tokensLidos, ref erros);

            arquivoSaida.WriteLine("Tabela Símbolos:\r\n" + 
                                    String.Join(Environment.NewLine, 
                                    tokensLidos.Select(token => String.Format("id: {0}, estado: {1}, rotulo: {2}, {3}", 
                                                                              token.identificador, 
                                                                              token.estadoReconhecedor.nome, 
                                                                              token.rotulo, 
                                                                              token.linha))));

            arquivoSaida.WriteLine("\n\nErros:\r\n" + (erros == null || erros.Count == 0 ? "\nNenhum.\n" : String.Join(Environment.NewLine, erros)));

            Console.WriteLine("\nAnálise léxica foi feita com sucesso...\n. . . . . . . .\n");
            arquivoSaida.WriteLine("Análise léxica foi feita com sucesso...\n");
            arquivoSaida.Close();
            arquivoSaida.Dispose();
            Console.WriteLine("Arquivo de saída gerado foi gerado.\n\n\n");
        }
    }
}
