namespace Setback.Learn

open System
open TorchSharp

module TensorBoard =

    /// TensorBoard log writer.
    let createWriter () =
        let timespan = DateTime.Now - DateTime.Today
        torch.utils.tensorboard.SummaryWriter(
            $"runs/run%05d{int timespan.TotalSeconds}")
