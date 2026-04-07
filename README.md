# 🎯 Inspetor AD

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/WPF-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="WPF"/>
  <img src="https://img.shields.io/badge/Status-Concluído-success?style=for-the-badge" alt="Status"/>
</p>

<p align="center">
  Uma ferramenta de monitoramento e inspeção de rede corporativa, focada em produtividade e resolução ágil de gargalos de tráfego.
</p>

---

## 📖 O Problema

O projeto nasceu de uma **necessidade real do dia a dia de infraestrutura e redes**. Após identificar picos de tráfego anormais no firewall da empresa, esbarramos em uma limitação técnica: o firewall informava apenas os **IPs** das máquinas que estavam consumindo a banda, sem nenhum contexto de usuário.

Descobrir manualmente qual dispositivo e qual colaborador estava gerando esse tráfego de forma descontrolada era um processo lento e ineficiente.

<img width="693" height="219" alt="image" src="https://github.com/user-attachments/assets/309c9a0e-36d2-40d8-bb72-1f3b7084980b" />


## 💡 A Solução

Desenvolvi o **Inspetor AD**, uma aplicação desktop moderna projetada para cruzar os dados dos IPs com o Active Directory (AD) da empresa em tempo real. A ferramenta permite identificar instantaneamente quem está logado em qual máquina, visualizar o status da conexão e resolver o problema de tráfego em minutos, não mais em horas.

---

## 🚀 Principais Funcionalidades

- **🔍 Consulta Híbrida:** Pesquise manualmente informando um ou mais IPs/Hostnames extraídos do firewall.
- **🌐 Integração Direta com Active Directory:** 
  - Varredura de todo o AD (Todos os computadores).
  - Varredura otimizada apenas em máquinas **Online**.
- **📊 Feedback Visual Intuitivo:** Uso de cores (Verde, Vermelho, Amarelo) baseado no status da comunicação (Online, Offline, Erros de RPC/WMI, Acesso Negado ou Máquina sem usuário).
- **⏱️ Desempenho e Assincronismo:** Interação não bloqueante com barra de progresso e botão de cancelamento seguro para varreduras extensas.
- **🎨 Interface Moderna:** UI construída em XAML com suporte a Dark Mode, proporcionando conforto visual e uma experiência de usuário profissional.

---

## 📸 Screenshots

<img width="888" height="828" alt="Captura de tela 2026-04-07 141910" src="https://github.com/user-attachments/assets/7deaf4e5-4728-420f-81a2-ba4ecd72b410" />

---

## 🛠️ Tecnologias e Arquitetura

- **Linguagem:** C#
- **Framework:** .NET 8.0 
- **Interface Gráfica:** WPF (Windows Presentation Foundation) com XAML Customizado
- **Integração:** Active Directory Services / WMI (Windows Management Instrumentation)
