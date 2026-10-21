## Cursor Hub 0.7.0

- Botoes de imagem agora usam ClickableContainer proprio, sem camadas de hover/flash do BasicButton.
- Nova pagina Configuracoes: tema claro/escuro (proxima abertura), animacoes, captura detalhada, limite de particulas e acesso ao ajuste de Hz.
- Importacao PNG/SVG para cursor e rastro. Imagens normalizadas, copiadas para os dados locais e reutilizadas durante o movimento.
- Editor de hotspot com transparencia em xadrez, mira arrastavel, centralizar, aplicar e cancelar. O hotspot e utilizado pelo cursor nativo do Windows.
- Presets e ajustes guardam imagens e hotspot.
- Captura do rastro preserva posicoes diferentes com o mesmo timestamp, antes descartadas pelo filtro temporal.

Baixe **CursorHub-0.7.0-win-x64.zip**, extraia toda a pasta e abra **CursorHub.exe**. Inclui runtime .NET. Feche a versao anterior primeiro.

Para usar imagens: Cursor > Importar cursor > posicionar hotspot; Rastro > Importar rastro. Salvar ajustes preserva as escolhas; Play aplica o cursor no desktop. Depois de editar um cursor ativo, pare e reative Play.

SVGs devem ser estaticos e autocontidos. A importacao prepara um bitmap com alfa; nao interpreta o SVG a cada quadro. Arquivos do tema do Windows nao sao modificados. Continua sendo pre-release; jogos em tela cheia exclusiva podem impedir sobreposicoes. Impacto e fidelidade em movimentos rapidos ainda precisam de medicao em hardware real. Avisos transitivos conhecidos de ImageSharp permanecem pendentes.
