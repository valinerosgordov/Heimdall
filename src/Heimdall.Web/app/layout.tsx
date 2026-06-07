import type { Metadata } from "next";
import { Chakra_Petch, Rajdhani, JetBrains_Mono, Orbitron } from "next/font/google";
import "uplot/dist/uPlot.min.css";
import "./globals.css";
import AppShell from "@/components/AppShell";

const chakra = Chakra_Petch({ subsets: ["latin"], weight: ["500", "600", "700"], variable: "--font-chakra" });
const rajdhani = Rajdhani({ subsets: ["latin"], weight: ["500", "600", "700"], variable: "--font-rajdhani" });
const jetbrains = JetBrains_Mono({ subsets: ["latin"], variable: "--font-jetbrains" });
const orbitron = Orbitron({ subsets: ["latin"], variable: "--font-orbitron" });

export const metadata: Metadata = {
  title: "Heimdall",
  description: "Server & project monitoring — the watchman of your infrastructure.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html
      lang="en"
      className={`${chakra.variable} ${rajdhani.variable} ${jetbrains.variable} ${orbitron.variable}`}
    >
      <body>
        <AppShell>{children}</AppShell>
      </body>
    </html>
  );
}
