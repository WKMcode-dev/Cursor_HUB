Versao de prototipo do novo Cursor Hub em C# e osu!framework.

## Como usar

1. Baixe **CursorHub-0.3.4-win-x64.zip** nos assets desta release.
2. Extraia toda a pasta e abra **CursorHub.exe**.
3. Escolha um preset e pressione **Play**. **Stop** ou **Ctrl+Shift+X** desliga o efeito.

O pacote inclui o runtime do .NET para Windows x64: nao precisa de .NET SDK nem Visual Studio. Mantenha as DLLs e a pasta Assets junto do executavel. Os links automaticos “Source code” sao apenas codigo-fonte.

## Incluido

- Janela personalizada com controles em bolinhas, bordas suavizadas e Play no recorte inferior.
- Presets com demonstracao, edicao, exclusao e persistencia local.
- Rastro global em camada transparente, sem capturar cliques, com verificacao de visibilidade e ordem topmost.
- Diagnostico executavel sem SDK e SHA256SUMS.txt para conferir o download.

O pipeline executa os testes do nucleo e valida as dependencias do pacote. A execucao visual em navegadores e jogos requer teste no computador do usuario. Tela cheia exclusiva e restricoes de sobreposicao nao tem suporte garantido. A dependencia transitiva ImageSharp 3.1.11 ainda possui avisos conhecidos de seguranca; a migracao compativel permanece pendente.
