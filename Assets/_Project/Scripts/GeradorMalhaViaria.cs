using System.Collections.Generic;
using _Project.Scripts.Core;
using UnityEngine;

// Define o namespace para organizar nosso código, como planejado.
namespace _Project.Scripts
{
    // Usamos o namespace Core para acessar nossas classes Intersection e Road.
    public class GeradorMalhaViaria : MonoBehaviour
    {
        [Header("Parâmetros do Terreno")]
        public Terrain terreno;

        [Header("Parâmetros de Geração (Random Walker)")]
        public int numeroDePassos = 50; // Antigo "numeroDeIteracoes"
        public float tamanhoDoPasso = 10.0f; // Distância que o agente anda a cada passo
        [Range(0, 45)]
        public float anguloMaximoDeCurva = 15.0f; // O quanto a direção pode mudar a cada passo

        // Listas para armazenar os dados da nossa malha gerada
        private readonly List<Intersection> _intersecoes = new List<Intersection>();
        private readonly List<Road> _ruas = new List<Road>();
        private int _proximoIdDisponivel;


        [ContextMenu("Gerar Malha Viária")]
        private void Gerar()
        {
            Debug.Log("Iniciando o processo de geração da malha viária...");
            LimparMalhaAnterior();
            ExecutarAlgoritmoRandomWalk();
        }

        [ContextMenu("Limpar Malha Viária")]
        private void LimparMalhaAnterior()
        {
            Debug.Log("Limpando malha anterior...");
            _intersecoes.Clear();
            _ruas.Clear();
            _proximoIdDisponivel = 0;
        }

        private void ExecutarAlgoritmoRandomWalk()
        {
            // Ponto de partida no centro do mapa
            Vector3 posicaoAtual = terreno.transform.position + new Vector3(terreno.terrainData.size.x / 2.0f, 0, terreno.terrainData.size.z / 2.0f);

            // Cria a primeira interseção
            Intersection intersecaoAnterior = new Intersection(ObterProximoId(), posicaoAtual);
            _intersecoes.Add(intersecaoAnterior);

            // Define uma direção inicial aleatória
            float anguloAtual = Random.Range(0, 360f);

            // Loop principal do algoritmo
            for (int i = 0; i < numeroDePassos; i++)
            {
                // 1. Altera levemente a direção
                anguloAtual += Random.Range(-anguloMaximoDeCurva, anguloMaximoDeCurva);

                // Converte o ângulo em um vetor de direção 2D (no plano XZ)
                Vector3 direcao = new Vector3(Mathf.Cos(anguloAtual * Mathf.Deg2Rad), 0, Mathf.Sin(anguloAtual * Mathf.Deg2Rad));

                // 2. Anda para frente
                posicaoAtual += direcao * tamanhoDoPasso;

                // 3. Cria a nova interseção
                Intersection novaIntersecao = new Intersection(ObterProximoId(), posicaoAtual);
                _intersecoes.Add(novaIntersecao);

                // 4. Cria a rua conectando a anterior com a nova
                Road novaRua = new Road(ObterProximoId(), intersecaoAnterior, novaIntersecao);
                _ruas.Add(novaRua);

                // Atualiza a interseção anterior para a próxima iteração
                intersecaoAnterior = novaIntersecao;
            }

            Debug.Log($"Geração concluída! {_intersecoes.Count} interseções e {_ruas.Count} ruas criadas.");
        }

        // Função auxiliar para garantir IDs únicos
        private int ObterProximoId()
        {
            return _proximoIdDisponivel++;
        }

        // Este método especial do Unity é chamado pelo editor para desenhar Gizmos na tela.
        // É perfeito para visualizar nossa malha sem precisar criar GameObjects.
        private void OnDrawGizmos()
        {
            if (_ruas == null || _ruas.Count == 0)
            {
                return;
            }

            // Desenha as ruas
            Gizmos.color = Color.white;
            foreach (var rua in _ruas)
            {
                Gizmos.DrawLine(rua.StartNode.Position, rua.EndNode.Position);
            }

            // Desenha as interseções
            Gizmos.color = Color.red;
            foreach (var intersecao in _intersecoes)
            {
                Gizmos.DrawSphere(intersecao.Position, 1.0f); // Esfera com raio de 1 metro
            }
        }
    }
}