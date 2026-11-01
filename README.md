# Fable 2: Return to Albion (Refii): Tradução PT-BR

Tradução completa para português do Brasil do Fable 2 rodando no **Refii Beta 6**. Inclui menus, missões, dicas, legendas e diálogos, cerca de 45 mil textos.
Os nomes próprios ficam no original. As conquistas continuam em inglês, porque vêm de outro arquivo do jogo.

**Download:** pegue o `traducao_ptbr_fable2.zip` em [Releases](../../releases) ou a pasta [`traducao_ptbr`](traducao_ptbr). As instruções estão no [`LEIAME.txt`](traducao_ptbr/LEIAME.txt). Depois de instalar, confira na tela de **Mods** do launcher se a tradução está **ativada**.

**Versão 1.2:** o comando de pular cerca/beirada ("Vault") aparecia como "Cofre" e agora aparece como "Pular". A versão do mod no launcher passa a mostrar 1.2.0. Para atualizar, repita os passos 1 e 2 do LEIAME, levando o `book.babel.original` da pasta da versão anterior para a nova antes do passo 2.

Este repositório não distribui nenhum arquivo do jogo. O script gera o `book.babel` traduzido a partir do `book.babel` original da sua própria cópia, e não mexe em saves nem no `refii.toml`.

## English

Complete Brazilian Portuguese translation for Fable 2 on **Refii Beta 6**.

- `traducao_ptbr/traducao_ptbr.f2mod` is the overlay for the launcher UI (`book.babel.csv`). Make sure it is enabled in the launcher's Mods screen.
- `traducao_ptbr/instalar_textos_jogo.ps1` + `BabelPatch.cs` rebuild the guest's `book.babel` from the user's own original. The guest reads `book.babel` from disk at boot and does not see the `.f2mod`. The output mirrors `Engine_BabelWriter`: blocks are capped at 18022 bytes and the tail is preserved.
- [`relato_bug/BUG_REPORT.md`](relato_bug/BUG_REPORT.md) is a crash report for the Refii team, about an intermittent guest access violation on the main thread right after menu actions, with log excerpts.

Known quirk: the villager title `%name% the %vocation%` goes through a path that turns non-ASCII into `?`, so profession names in this translation avoid accents.

Feito por Douglas ([@dscm-spec](https://github.com/dscm-spec)) com Claude (Anthropic).
