"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
Object.defineProperty(exports, "__esModule", { value: true });
const sequelize_typescript_1 = require("sequelize-typescript");
const mirroredvideo_1 = require("./mirroredvideo");
var CommentStatus;
(function (CommentStatus) {
    CommentStatus[CommentStatus["AwaitingUpdate"] = 5] = "AwaitingUpdate";
    CommentStatus[CommentStatus["Current"] = 10] = "Current";
})(CommentStatus = exports.CommentStatus || (exports.CommentStatus = {}));
let CommentReply = class CommentReply extends sequelize_typescript_1.Model {
};
__decorate([
    sequelize_typescript_1.Column({
        allowNull: false,
        unique: true
    }),
    __metadata("design:type", String)
], CommentReply.prototype, "redditPostId", void 0);
__decorate([
    sequelize_typescript_1.Column({
        allowNull: false,
        unique: false
    }),
    __metadata("design:type", Number)
], CommentReply.prototype, "status", void 0);
__decorate([
    sequelize_typescript_1.CreatedAt,
    __metadata("design:type", Date)
], CommentReply.prototype, "createdAt", void 0);
__decorate([
    sequelize_typescript_1.UpdatedAt,
    __metadata("design:type", Date)
], CommentReply.prototype, "updatedAt", void 0);
__decorate([
    sequelize_typescript_1.DeletedAt,
    __metadata("design:type", Date)
], CommentReply.prototype, "deletedAt", void 0);
__decorate([
    sequelize_typescript_1.HasMany(() => mirroredvideo_1.MirroredVideo),
    __metadata("design:type", Array)
], CommentReply.prototype, "mirroredVideos", void 0);
CommentReply = __decorate([
    sequelize_typescript_1.Table({
        timestamps: true
    })
], CommentReply);
exports.CommentReply = CommentReply;
//# sourceMappingURL=commentreply.js.map