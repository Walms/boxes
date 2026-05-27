{
  description = "BoxTracker — home moving box tracking app (F# / Fable / Saturn)";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.05";
  };

  outputs =
    { self, nixpkgs }:
    let
      system = "x86_64-linux";
      pkgs = nixpkgs.legacyPackages.${system};
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        name = "boxtracker";

        packages = with pkgs; [
          dotnet-sdk_10
          nodejs_20
          nodePackages.npm
        ];

        env.DOTNET_CLI_TELEMETRY_OPTOUT = "1";
      };
    };
}
