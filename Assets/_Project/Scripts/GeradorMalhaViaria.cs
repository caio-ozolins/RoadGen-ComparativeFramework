using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Generation;

namespace _Project.Scripts
{
    public class GeradorMalhaViaria : MonoBehaviour
    {
        [Header("Configuração do Terreno")]
        public Terrain terreno;

        [Header("Parâmetros de Geração (Random Walker)")]
        public int numeroDePassos = 50;
        public float tamanhoDoPasso = 10.0f;
        [Range(0, 45)]
        public float anguloMaximoDeCurva = 15.0f;
        [Range(0, 90)]
        public float inclinacaoMaxima = 30.0f;
        [Range(0, 10)]
        public int maxTentativasDeDesvio = 5;

        private readonly List<Intersection> _intersecoes = new List<Intersection>();
        private readonly List<Road> _ruas = new List<Road>();

        [ContextMenu("Gerar Malha Viária")]
        private void Gerar()
        {
            Debug.Log("Orquestrador iniciando a geração...");
            LimparMalhaAnterior();
            
            // Cria e configura a instância do nosso algoritmo em um único bloco.
            var gerador = new RandomWalkGerador
            {
                Terreno = this.terreno,
                NumeroDePassos = this.numeroDePassos,
                TamanhoDoPasso = this.tamanhoDoPasso,
                AnguloMaximoDeCurva = this.anguloMaximoDeCurva,
                InclinacaoMaxima = this.inclinacaoMaxima,
                MaxTentativasDeDesvio = this.maxTentativasDeDesvio
            };
            
            var resultado = gerador.Gerar();
            
            _intersecoes.AddRange(resultado.intersecoes);
            _ruas.AddRange(resultado.ruas);
        }

        [ContextMenu("Limpar Malha Viária")]
        private void LimparMalhaAnterior()
        {
            Debug.Log("Limpando malha anterior...");
            _intersecoes.Clear();
            _ruas.Clear();
        }
        
        private void OnDrawGizmos()
        {
            if (_ruas == null || _ruas.Count == 0)
                return;

            Gizmos.color = Color.white;
            foreach (var rua in _ruas)
            {
                Gizmos.DrawLine(rua.StartNode.Position, rua.EndNode.Position);
            }

            Gizmos.color = Color.red;
            foreach (var intersecao in _intersecoes)
            {
                Gizmos.DrawSphere(intersecao.Position, 1.0f);
            }
        }
    }
}