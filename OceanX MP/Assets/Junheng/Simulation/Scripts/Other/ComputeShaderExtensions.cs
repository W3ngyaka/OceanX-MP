using UnityEngine;

namespace OceanX
{
    public static class ComputeShaderExtensions
    {
        public static void DispatchKernel(this ComputeShader computeShader, string kernelFunctionName, int numberOfComputations)
        {
            int kernelID = computeShader.FindKernel(kernelFunctionName);
            computeShader.DispatchThreads(kernelID, numberOfComputations);
        }

        public static void DispatchKernel(this ComputeShader computeShader, string kernelFunctionName, int numberOfComputationsX, int numberOfComputationsY)
        {
            int kernelID = computeShader.FindKernel(kernelFunctionName);
            computeShader.DispatchThreads(kernelID, numberOfComputationsX, numberOfComputationsY);
        }

        public static void DispatchThreads(this ComputeShader computeShader, int kernelID, int numberOfComputations)
        {
            computeShader.GetKernelThreadGroupSizes(kernelID, out uint x, out _, out _);
            int numberOfThreadGroups = (numberOfComputations + (int)x - 1) / (int)x;
            computeShader.Dispatch(kernelID, numberOfThreadGroups, 1, 1);
        }

        public static void DispatchThreads(this ComputeShader computeShader, int kernelID, int numberOfComputationsX, int numberOfComputationsY)
        {
            computeShader.GetKernelThreadGroupSizes(kernelID, out uint x, out uint y, out _);
            int numberOfThreadGroupsX = (numberOfComputationsX + (int)x - 1) / (int)x;
            int numberOfThreadGroupsY = (numberOfComputationsY + (int)y - 1) / (int)y;
            computeShader.Dispatch(kernelID, numberOfThreadGroupsX, numberOfThreadGroupsY, 1);
        }
    }
}
