## Cursor Hub 0.6.0

- Remove o fundo retangular de hover dos botoes com SVG.
- Icones: lixeira em vermelho, editar em dourado e usar em verde.
- Nova Demonstration_Area.svg em ciano/grafite, sem a moldura alta do conceito anterior.
- Titulos decorativos removidos.
- Limite ajustavel entre 30 e 360Hz, com atalhos 60/75/144/165Hz e persistencia. O valor e um teto, nao uma garantia de FPS.
- Recupera ate 64 posicoes reais do historico do Windows entre quadros, na previa e no desktop, com fallback para a posicao atual. Evita reconstruir um arco inteiro apenas pela reta entre as pontas. Buffer fixo de 512 particulas e espacamento mais fino.
- Espera de alta resolucao quando disponivel, sem busy-wait nem mudanca global da resolucao de temporizadores.

Baixe **CursorHub-0.6.0-win-x64.zip**, extraia toda a pasta e abra **CursorHub.exe**. Runtime .NET incluido, sem SDK ou Visual Studio. Feche a versao anterior antes de iniciar.

A captura depende do historico disponivel no Windows e do mouse; movimentos nao registrados nao podem ser recuperados. Testes sinteticos verificam arco, limites, alfa e persistencia; o pipeline tambem abre a UI e verifica recorte, 8px ao maximizar/restaurar e composicao global. Desempenho e fidelidade em movimentos humanos e jogos ainda precisam de medicao em hardware real. Tela cheia exclusiva pode impedir o rastro. Avisos transitivos de ImageSharp permanecem pendentes.
